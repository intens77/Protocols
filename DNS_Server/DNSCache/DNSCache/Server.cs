using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DNSCache
{
    public static class Server
    {
        private const int port = 80;
        private static string IpAddress = "127.0.0.1";
        private static bool close = false;

        private static Cache cache = new Cache();

        public static void Main(string[] args)
        {
            Console.WriteLine("Сервер запущен\nНажмите Enter, чтобы принять запрос\nВведите 0, чтобы остановить работу");
            
            var work = new Thread(Work);
            work.Start();
            var input = Console.ReadLine();
            while (input != "0")
            {
                input = Console.ReadLine();
            }
            cache.StopWork();
            close = true;
        }

        private static void Work()
        {
            while (!close)
            {
                ReceiveMessage();
            }
        }

        private static void ReceiveMessage()
        {
            UdpClient receiver = new UdpClient(port); // UdpClient для получения данных
            IPEndPoint remoteIp = null; // адрес входящего подключения
            try
            {
                byte[] data = receiver.Receive(ref remoteIp); // получаем данные
                Console.WriteLine("Получен запрос\nПарсинг запроса");
                int index = 0;
                var hostAndType = getHostAndType(data, ref index);
                if (cache.Contains(hostAndType))
                {
                    Console.WriteLine("Запись взята из кеша");
                    SendAnswer(cache.getAnswer(hostAndType));
                }
                else
                {
                    Console.WriteLine("Запись в кеше не найдена, разрешение запроса...");
                    var ans = resolve(data);
                    ParseAnswer(ans);
                    SendAnswer(ans);
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                receiver.Close();
            }
        }

        private static void SendAnswer(byte[] data)
        {
            UdpClient sender = new UdpClient();
            try
            {
                sender.Send(data, data.Length, IpAddress, 54);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                sender.Close();
            }
        }

        private static byte[] getLabel(byte[] req, int index)
        {
            var length = req[index];
            var result = req.Skip(index+1).Take(length).ToArray();
            return result;
        }

        private static byte[] getHostAndType(byte[] req, ref int outIndex , int startIndex = 12)
        {
            int index = startIndex;
            var result = new List<byte>();
            while(req[index] != 0x00)
            {
                var length = req[index];
                if (length == 192) //значит это ссылка, первые два бита единицы
                {
                    var host = getHostAndType(req, ref outIndex, req[index + 1]);
                    host = host.Take(host.Length-2).ToArray();
                    result.AddRange(host);
                    index += 1;
                    break;
                }
                else result.AddRange(getLabel(req, index));
                index += length + 1;
            }

            //Добавляем тип
            result.Add(req[index + 1]);
            result.Add(req[index + 2]);
            outIndex = index + 5;//индекс начала записи ttl в ответе 
            return result.ToArray();
        }

        public static byte[] resolve(byte[] request)
        {
            IPEndPoint IpDNS = new IPEndPoint(new IPAddress(new byte[] { 8, 8, 8, 8 }), 53);//гугл
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000);
            s.Connect(IpDNS);
            s.SendTo(request, request.Length, SocketFlags.None, IpDNS);

            byte[] rec = new byte[1500];

            s.Receive(rec);
            return rec;
        }

        public static void ParseAnswer(byte[] ans)
        {
            //сначала парсим зону Answers
            int countAnswers = (int)(ans[6] * Math.Pow(2, 8) + ans[7]);
            int countAuthority = (int)(ans[8] * Math.Pow(2, 8) + ans[9]);
            int countAdditional = (int)(ans[10] * Math.Pow(2, 8) + ans[11]);
            int startIndex = 12; //найдем начало зоны
            var hostType = getHostAndType(ans, ref startIndex);
            if (!cache.Contains(hostType))
                cache.add(hostType, ans, 30);
            var ansIndex = startIndex;
            ParseAnswerZone(ans, countAnswers, startIndex, ref startIndex);
            ParseAuthorityZone(ans, countAuthority, startIndex, ansIndex, ref startIndex);
            ParseAdditionalZone(ans, countAdditional, startIndex, ansIndex, ref startIndex);

        }

        private static void ParseAnswerZone(byte[] ans, int count, int startIndex, ref int lastIndex)
        {
            int endIndex = startIndex;
            while (count != 0)
            {
                var prefix = ans.Take(startIndex).ToArray();
                prefix[6] = 0x00; prefix[7] = 0x01;
                prefix[8] = prefix[9] = prefix[10] = prefix[11] = 0x00;//обнулил колличества записей кроме ансверс

                var hostAndType = getHostAndType(ans, ref endIndex, startIndex);
                var hostTypePrefix = ans.Skip(startIndex).Take(endIndex - startIndex).ToArray();

                var clazz = new byte[2] { ans[endIndex-2], ans[endIndex-1]};

                var byteTTL = new byte[4] { ans[endIndex], ans[endIndex + 1], ans[endIndex + 2], ans[endIndex + 3] };
                var ttl = Convert.ToInt32(ans[endIndex] + "" + ans[endIndex + 1] + ans[endIndex + 2] + ans[endIndex + 3]);

                var byteDataLen = new byte[2] { ans[endIndex + 4], ans[endIndex + 5] };
                var dataLength = Convert.ToInt32(ans[endIndex + 4] + "" + ans[endIndex + 5]);
                endIndex += 6;
                var currIndex = 0;
                byte[] data = new byte[dataLength];
                while (currIndex != dataLength)
                {
                    data[currIndex] = ans[endIndex + currIndex];
                    currIndex++;
                }
                endIndex += currIndex;
                startIndex = endIndex;
                count--;

                if (!cache.Contains(hostAndType))
                {
                    data = prefix.Concat(hostTypePrefix).Concat(byteTTL).Concat(byteDataLen).Concat(data).ToArray();
                    cache.add(hostAndType, data, ttl);
                    Console.WriteLine("Запись добавлена в кеш");
                }

            }
            lastIndex = endIndex;
            Console.WriteLine("Распарсена зона Answers");
        }

        private static void ParseAuthorityZone(byte[] ans, int count, int startAnswersZoneIndex, int startIndex, ref int lastIndex)
        {
            int endIndex = startIndex;
            while (count != 0)
            {
                var prefix = ans.Take(startAnswersZoneIndex).ToArray();
                prefix[6] = 0x00; prefix[7] = 0x01;
                prefix[8] = prefix[9] = prefix[10] = prefix[11] = 0x00;
                var hostAndType = getHostAndType(ans, ref endIndex, startIndex);
                var hostTypePrefix = ans.Skip(startIndex).Take(endIndex - startIndex).ToArray();
                var clazz = new byte[2] { ans[endIndex - 2], ans[endIndex - 1] };
                var byteTTL = new byte[4] { ans[endIndex], ans[endIndex + 1], ans[endIndex + 2], ans[endIndex + 3] };
                var ttl = Convert.ToInt32(ans[endIndex] + "" + ans[endIndex + 1] + ans[endIndex + 2] + ans[endIndex + 3]);
                var byteDataLen = new byte[2] { ans[endIndex + 4], ans[endIndex + 5] };
                var dataLength = Convert.ToInt32(ans[endIndex + 4] + "" + ans[endIndex + 5]);
                endIndex += 6;
                var currIndex = 0;
                byte[] data = new byte[dataLength];
                while (currIndex != dataLength)
                {
                    data[currIndex] = ans[endIndex + currIndex];
                    currIndex++;
                }
                endIndex += currIndex;
                startIndex = endIndex;
                count--;

                if (!cache.Contains(hostAndType))
                {
                    data = prefix.Concat(hostTypePrefix).Concat(byteTTL).Concat(byteDataLen).Concat(data).ToArray();
                    cache.add(hostAndType, data, ttl);
                    Console.WriteLine("Запись добавлена в кеш");
                }
            }
            lastIndex = endIndex;
            Console.WriteLine("Распарсена зона Authority");
        }

        private static void ParseAdditionalZone(byte[] ans, int count, int startAnswersZoneIndex, int startIndex, ref int lastIndex)
        {
            int endIndex = startIndex;
            while (count != 0)
            {
                while (count != 0)
                {
                    var prefix = ans.Take(startAnswersZoneIndex).ToArray();
                    prefix[6] = 0x00; prefix[7] = 0x01;
                    prefix[8] = prefix[9] = prefix[10] = prefix[11] = 0x00;
                    var hostAndType = getHostAndType(ans, ref endIndex, startIndex);
                    var hostTypePrefix = ans.Skip(startIndex).Take(endIndex - startIndex).ToArray();
                    var clazz = new byte[2] { ans[endIndex - 2], ans[endIndex - 1] };
                    var byteTTL = new byte[4] { ans[endIndex], ans[endIndex + 1], ans[endIndex + 2], ans[endIndex + 3] };
                    var ttl = Convert.ToInt32(ans[endIndex] + "" + ans[endIndex + 1] + ans[endIndex + 2] + ans[endIndex + 3]);
                    var byteDataLen = new byte[2] { ans[endIndex + 4], ans[endIndex + 5] };
                    var dataLength = Convert.ToInt32(ans[endIndex + 4] + "" + ans[endIndex + 5]);
                    endIndex += 6;
                    var currIndex = 0;
                    byte[] data = new byte[dataLength];
                    while (currIndex != dataLength)
                    {
                        data[currIndex] = ans[endIndex + currIndex];
                        currIndex++;
                    }
                    endIndex += currIndex;
                    startIndex = endIndex;
                    count--;

                    if (!cache.Contains(hostAndType))
                    {
                        data = prefix.Concat(hostTypePrefix).Concat(byteTTL).Concat(byteDataLen).Concat(data).ToArray();
                        cache.add(hostAndType, data, ttl);
                        Console.WriteLine("Запись добавлена в кеш");
                    }
                }
                lastIndex = endIndex;
                Console.WriteLine("Распарсена зона Additional");
            }
        }
    }
}
