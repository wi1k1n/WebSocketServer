using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections.Generic;

namespace WebSocketServer
{
    /// <summary>
    /// Используется для прослушки входящего от клиента потока, парсинга получаемых данных и возвращения сформированных пакетов
    /// </summary>
    class WSListener
    {
        public delegate void WSDataReceivedEventHandler (object sender, WSDataReceivedEventArgs e);

        public event EventHandler OnStarted;
        public event WSDataReceivedEventHandler OnSinglePacketReceived;
        public event WSDataReceivedEventHandler OnFragmentReceived;
        public event WSDataReceivedEventHandler OnFragmentedPacketReceived;

        private NetworkStream stream;
        private byte[] buffer; // Буфер, в который происходит считывание данных из потока
        private byte[] restBuffer; // Буфер, в который скидываются данные, считанные из потока, но принадлежащие следующему пакету
        private int bufferLength = 1024; // Не меньше 16, иначе вылетит при попытке собрать из 10-байтного массива UInt64
        private WSPacket packet;
        private List<WSPacket> packetFragments;
        private bool listen = false;

        public bool IsListening { get { return listen; } }

        // Переменные статуса, связывающие 2 последовательных обработки прочитанных из потока данных
        private bool awaitingBuffer = false; // Показывает, текущие данные являются новым пакетом, или продолжением предыдущего
        private bool awaitingPacket = false; // Показывает, текущие данные являются новым пакетом/фрагментом, или обрабатываются как следующий фрагмент предыдущего
        private bool awB { get { return awaitingBuffer; } set { awaitingBuffer = value; } }
        private bool awP { get { return awaitingPacket; } set { awaitingPacket = value; } }

        public WSListener (NetworkStream stream)
        {
            this.stream = stream;
            buffer = new byte[bufferLength];
            restBuffer = new byte[0];
        }

        // Стартует прослушку, читает поток, дергает события при получении данных
        public void Start ()
        {
            listen = true;
            onStarted();
            using (Stream str = stream)
            {
                // В бесконечном цикле читаем поток
                int cnt = 0;
                while (listen)
                {
                    try
                    {
                        // Читаем из потока в буфер и формируем массив соответствующей длины
                        // Если есть данные, оставшиеся на обработку с предыдущего витка,
                        // то они "подсовываются" перед данными из буфера
                        cnt = 0;
                        if (stream.DataAvailable && restBuffer.Length < buffer.Length * 2 || restBuffer.Length == 0) // Вторая проверка для того, чтобы убрать расходимость. Она нужна когда буфер достаточно большой, а сервер очень сильно загружен большим количеством достаточно малых пакетов. Третья проверка нужна для того, чтобы остановить цикл на стадии чтения из потока, чтобы не тратить ресурсы на бесполезные прогоны следующих ресурсоемких операций
                            cnt = stream.Read(buffer, 0, bufferLength);
                        byte[] rawData = new byte[cnt + restBuffer.Length];
                        restBuffer.CopyTo(rawData, 0);
                        for (int i = 0; i < cnt; i++)
                            rawData[i + restBuffer.Length] = buffer[i];
                        cnt += restBuffer.Length;
                        restBuffer = new byte[0];

                        if (cnt < 2)
                            continue;

                        // Далее массив считанных данных идет на распределение обработчиков в зависимости от статуса
                        if (awB)
                        {
                            // Данный обраотчик работает, если текущий пакет является недосчитанным из потока
                            // продолжением предыдущего (т.е. предыдущий пакет считан не полностью)

                            // Выдвигаем гипотезу, что весь текущий пакет (размера cnt) принадлежит предыдущему
                            byte[] temp = new byte[packet.Data.Length + cnt];
                            int bound = cnt; // Границы, до которой впоследствии будет перебирать for {..}

                            // Проверяем гипотезу
                            if (packet.Length < (uint)(cnt + packet.Data.Length))
                            {
                                // Если заявленный размер пакета меньше, чем уже обработанная его часть
                                // вместе со ВСЕМИ прочитанными на данной итерации while'а данными,
                                // то это означает, что гипотеза неверна, а следовательно в текущих данных присутствует
                                // часть предыдущего пакета и часть следующего

                                // Поэтому корректируем длину массива итоговых данных temp
                                temp = new byte[packet.Length];
                                // Меняем границу (т.к. для варианта, когда ВСЕ текущие данные принадлежат предыдущему пакету
                                // формула вычисления границы for'а другая)
                                bound = (int)((uint)packet.Length - packet.Data.Length);

                                // И заполняем буфер "чужих" данных, которые добавятся в обработку на следующей итерации while'а
                                restBuffer = new byte[cnt - bound];
                                for (int i = 0; i < restBuffer.Length; i++)
                                    restBuffer[i] = rawData[i + bound];
                            }

                            // Копируем в temp уже имеющиеся у нас данные из текущего пакета
                            packet.Data.CopyTo(temp, 0);
                            // И дополняем новыми, пришедшими из текущей порции данных, попутно
                            // применяя к ним маску, если бит MaskFlag указывает на эту операцию
                            for (int i = 0; i < bound; i++)
                                temp[i + packet.Data.Length] = packet.MaskFlag ? (byte)(rawData[i] ^ packet.Mask[(packet.Data.Length + i) % 4]) : rawData[i];
                            packet.Data = temp;

                            // Контрольная проверка размеров данных, после которой убирается
                            // флаг awaitingBuffer, обозначающий, что следующий пакет пойдет к обработчику новых пакетов,
                            // и дергается событие успешного приема пакета
                            if ((uint)packet.Data.Length == packet.Length)
                            {
                                awB = false;
                                if (packet.Fin)
                                {
                                    if (awP)
                                    {
                                        awP = false;
                                        packetFragments.Add(packet);
                                        onFragmentedPacketReceived();
                                    } else
                                    {
                                        packetFragments = new List<WSPacket>(1);
                                        packetFragments.Add(packet);
                                        onSinglePacketReceived();
                                    }
                                } else
                                {
                                    if (awP)
                                    {
                                        packetFragments.Add(packet);
                                        onFragmentReceived();
                                    } else
                                    {
                                        awP = true;
                                        packetFragments = new List<WSPacket>();
                                        packetFragments.Add(packet);
                                        onSinglePacketReceived();
                                    }
                                }
                            } else if (packet.Length < (uint)(packet.Data.Length))
                                throw new Exception("Обнаружено несоответствие размера, указанного в заголовке пакета с реальным размером полученных данных: заявленный размер меньше полученного!");
                        } else
                        {
                            // Данный обработчик работает, если текущий пакет является новым, т.е. в первую очередь
                            // обрабатывается информация из заголовков

                            // Заголовки считываются в соответствии со спецификацией пакетов WebSocket'а
                            packet = new WSPacket();
                            packet.Fin = ByteManager.getBit(rawData[0], 0);
                            packet.Rsv1 = ByteManager.getBit(rawData[0], 1);
                            packet.Rsv2 = ByteManager.getBit(rawData[0], 2);
                            packet.Rsv3 = ByteManager.getBit(rawData[0], 3);
                            packet.Opcode = !awP ? ByteManager.constructByte(rawData[0], 4, 7) : packet.Opcode; // Стоит проверка для того, чтобы не перезаписать нулями уже записанный опкод
                            packet.MaskFlag = ByteManager.getBit(rawData[1], 0);
                            int cursor = 2;
                            packet.Length = getLength(rawData, ref cursor); // При вычислении длины в UInt64 может быть исключение, если размер буфера buffer не достаточно велик
                            packet.Mask = getMask(rawData, ref cursor);
                            packet.HeadersLength = cursor;

                            if (awP)
                            {
                                // TODO: Организовать прием дефрагментированных данных
                                byte[] temp = new byte[cnt - cursor];

                                // Проверяем гипотезу
                                if (packet.Length < (uint)(cnt - cursor))
                                {
                                    // Если заявленный размер пакета меньше размеров, полученных данных,
                                    // то меняем размер массива temp
                                    temp = new byte[packet.Length];
                                    // Оставшиеся "чужие" данные отправляем в буфер, из которого они будут
                                    // отправлены вместе со следующей порцией данных
                                    restBuffer = new byte[(uint)(cnt - cursor) - packet.Length];
                                    for (int i = 0; i < restBuffer.Length; i++)
                                        restBuffer[i] = rawData[(uint)(i + cursor) + packet.Length];
                                }

                                // Перебираем все данные, попутно применяя к ним маску, где это необходимо
                                for (uint i = 0; i < temp.Length; i++)
                                    temp[i] = packet.MaskFlag ? (byte)(rawData[i + cursor] ^ packet.Mask[i % 4]) : rawData[i + cursor];
                                packet.Data = temp;

                                if ((uint)packet.Data.Length < packet.Length)
                                {
                                    awB = true;
                                } else if ((uint)packet.Data.Length == packet.Length)
                                {
                                    packetFragments.Add(packet);
                                    if (packet.Fin)
                                    {
                                        awP = false;
                                        onFragmentedPacketReceived();
                                    } else
                                        onFragmentReceived();
                                } else
                                    throw new Exception("Обнаружено несоответствие размера, указанного в заголовке пакета с реальным размером полученных данных: заявленный размер меньше полученного!");
                            } else
                            {
                                // Данная секция предназначена для обработки нефрагментированных данных. Т.к. если данные передаются фрагментами
                                // то в конце каждой итерации while'а awP будет выставляться на 1, пока не будет достигнут конечный фрагемент.
                                // Фрагментированные данные обрабатываются секцией выше

                                if (!packet.Fin)
                                {
                                    Console.ForegroundColor = ConsoleColor.DarkRed;
                                    Console.WriteLine("Данные дефрагментированы!");
                                    Console.ForegroundColor = ConsoleColor.Gray;
                                }

                                // Выдвигаем гипотизу, что ВСЕ полученные данные принадлежат текущему пакету
                                byte[] temp = new byte[cnt - cursor];

                                // Проверяем гипотезу
                                if (packet.Length < (uint)(cnt - cursor))
                                {
                                    // Если заявленный размер пакета меньше размеров, полученных данных,
                                    // то меняем размер массива temp
                                    temp = new byte[packet.Length];
                                    // Оставшиеся "чужие" данные отправляем в буфер, из которого они будут
                                    // отправлены вместе со следующей порцией данных
                                    restBuffer = new byte[(uint)(cnt - cursor) - packet.Length];
                                    for (int i = 0; i < restBuffer.Length; i++)
                                        restBuffer[i] = rawData[(uint)(i + cursor) + packet.Length];
                                }

                                // Перебираем все данные, попутно применяя к ним маску, где это необходимо
                                for (uint i = 0; i < temp.Length; i++)
                                    temp[i] = packet.MaskFlag ? (byte)(rawData[i + cursor] ^ packet.Mask[i % 4]) : rawData[i + cursor];
                                packet.Data = temp;

                                if ((uint)packet.Data.Length < packet.Length)
                                {
                                    awB = true;
                                } else if ((uint)packet.Data.Length == packet.Length)
                                {
                                    if (packet.Fin)
                                    {
                                        packetFragments = new List<WSPacket>(1);
                                        packetFragments.Add(packet);
                                        onSinglePacketReceived();
                                    } else
                                    {
                                        awP = true;
                                        packetFragments = new List<WSPacket>();
                                        packetFragments.Add(packet);
                                        onFragmentReceived();
                                    }
                                } else
                                    throw new Exception("Обнаружено несоответствие размера, указанного в заголовке пакета с реальным размером полученных данных: заявленный размер меньше полученного!");
                            }
                        }
                    } catch { }
                }
            }
        }
        // Останавливаем поток (жесткая остановка без отправки close пакета)
        public void Stop ()
        {
            listen = false;
            stream.Dispose();
            buffer = null;
            restBuffer = null;
            //packet = null;
            packetFragments = null;
        }

        // Получаем заявленную в пакете длину из полученных RAW-данных в массиве msg
        private UInt64 getLength (byte[] msg, ref int cursor)
        {
            UInt64 res = ByteManager.constructByte(msg[1], 1, 7);
            cursor = 2;
            if (res == 126)
                res = ByteManager.Int16FromBytes(msg[cursor++], msg[cursor++]);
            else if (res == 127)
            {
                res = ByteManager.Int64FromBytes(msg, 2);
                cursor = 10;
            }

            return res;
        }
        // Получаем маску для расшифровки данных пакета
        private byte[] getMask (byte[] msg, ref int cursor)
        {
            byte[] res = null;
            if (packet.MaskFlag)
            {
                res = new byte[4];
                for (int i = 0; i < 4; i++)
                    res[i] = msg[cursor++];
            }
            return res;
        }

        #region EventHandlers
        private void onStarted ()
        {
            if (OnStarted != null)
                OnStarted(this, new EventArgs());
        }
        private void onSinglePacketReceived ()
        {
            if (OnSinglePacketReceived != null)
            {
                OnSinglePacketReceived(this, new WSDataReceivedEventArgs(packetFragments));
            }
        }
        private void onFragmentReceived ()
        {
            if (OnFragmentReceived != null)
            {
                OnFragmentReceived(this, new WSDataReceivedEventArgs(packetFragments, false));
            }
        }
        private void onFragmentedPacketReceived ()
        {
            if (OnFragmentedPacketReceived != null)
            {
                OnFragmentedPacketReceived(this, new WSDataReceivedEventArgs(packetFragments, false));
            }
        }
        #endregion
    }

    // Класс аргументов для делегата WSDataReceived, передаваемых классу Client
    class WSDataReceivedEventArgs
    {
        private List<WSPacket> packetFragments;
        public List<WSPacket> PacketFragments { get { return packetFragments; } }
        public WSPacket LastPacket { get { return packetFragments[packetFragments.Count - 1]; } }
        public bool SinglePacket { get; private set; }

        public WSDataReceivedEventArgs (List<WSPacket> packetFragments, bool singlePacket = true)
        {
            this.packetFragments = packetFragments;
            SinglePacket = singlePacket;
        }
    }
}