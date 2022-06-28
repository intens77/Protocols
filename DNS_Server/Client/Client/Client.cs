using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DNSCache
{
    public static class Client
    {
        private static string ip = "127.0.0.1";
        private static int port = 80;
        private static string hostName = "habrahabr.ru";
        private static byte[] type = new byte[2] { 0x00, 0x01 };//по умолчанию тип А

        public static void Main(string[] args)
        {
            Console.WriteLine("Клиент запущен...\nВведите имя хоста и тип через пробел\nнажмите Enter, чтобы сделать запрос\nВведите 0, чтобы завершить работу сервера");
            var input = Console.ReadLine();
            while (input != "0")
            {
                hostName = input.Split(" ")[0];
                type = getType(input.Split(" ")[1]);
                MakeRequest();
                Console.WriteLine("Запрос отправлен");
                WaitAnswer();
                input = Console.ReadLine();
            }
        }

        private static byte[] getType(string str)
        {
            switch (str.ToUpper())
            {
                case "A":
                    return new byte[2] { 0x00, 0x01};
                case "AAAA":
                    return new byte[2] { 0x00, 0x1c };
                case "CNAME":
                    return new byte[2] { 0x00, 0x05 };
                case "MX":
                    return new byte[2] { 0x00, 0x0f };
                case "NS":
                    return new byte[2] { 0x00, 0x02 };
                case "SOA":
                    return new byte[2] { 0x00, 0x06 };
                default:
                    return new byte[2] { 0x00, 0x01 };
            }
        }

        private static void WaitAnswer()
        {
            UdpClient receiver = new UdpClient(54); // UdpClient для получения данных
            IPEndPoint remoteIp = new IPEndPoint(IPAddress.Parse(ip), port); // адрес входящего подключения
            try
            {
                byte[] data = receiver.Receive(ref remoteIp); // получаем данные
                Console.WriteLine("Получен ответ\nПарсинг ответа");
                foreach (var n in ParseAnswer(data))
                    Console.WriteLine(n);

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

        private static string[] ParseAnswer(byte[] data)
        {
            int countAnswers = (int)(data[6] * Math.Pow(2, 8) + data[7]);
            int startIndex = 12; //найдем начало зоны
            while (data[startIndex] != 0x00)
            {
                startIndex++;
            }
            startIndex += 5;

            return ParseAnswerZone(data, countAnswers, startIndex, ref startIndex);
        }

        private static string[] ParseAnswerZone(byte[] ans, int count, int startIndex, ref int lastIndex)
        {
            var result = new string[count];
            int endIndex = startIndex;
            while (count != 0)
            {
                var hostAndType = getHostAndType(ans, ref endIndex, startIndex);
                var clazz = new byte[2] { ans[endIndex - 2], ans[endIndex - 1] };
                var byteTTL = new byte[4] { ans[endIndex], ans[endIndex + 1], ans[endIndex + 2], ans[endIndex + 3] };
                var ttl = Convert.ToInt32(ans[endIndex] + "" + ans[endIndex + 1] + ans[endIndex + 2] + ans[endIndex + 3]);
                var byteDataLen = new byte[2] { ans[endIndex + 4], ans[endIndex + 5] };
                var dataLength = Convert.ToInt32(ans[endIndex + 4] + "" + ans[endIndex + 5]);
                endIndex += 6;
                var indexOfStartData = endIndex;
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

                var type = Convert.ToInt32(hostAndType.Skip(hostAndType.Length - 1).ToArray()[0]);
                var sType = "";
                switch(type)
                {
                    case 1: sType = "A";
                        break;
                    case 28: sType = "AAAA";
                        break;
                    case 5: sType = "CNAME";
                        break;
                    case 15: sType = "MX";
                        break;
                    case 2: sType = "NS";
                        break;
                    case 6: sType = "SOA";
                        break;
                    default:  sType = "unknown";
                        break;

                }
                var sIP = "";
                if (sType == "A" || sType == "MX")
                {
                    sIP = Convert.ToInt32(data[0]) + "." + Convert.ToInt32(data[1]) + "." + Convert.ToInt32(data[2]) + "." + Convert.ToInt32(data[3]);
                }
                if (sType == "NS")
                {
                    var hostName = getHostAndType(ans, ref indexOfStartData, indexOfStartData);
                    hostName = hostName.Take(hostName.Length - 2).ToArray();
                    sIP = Encoding.ASCII.GetString(hostName);
                }

                result[count] = Encoding.ASCII.GetString(hostAndType.Take(hostAndType.Length - 2).ToArray()) + " type= " +
                                  sType + " " + sIP;

            }
            lastIndex = endIndex;
            Console.WriteLine("Распарсена зона Answers");
            return result;
        }

        private static byte[] getLabel(byte[] req, int index)
        {
            var length = req[index];
            var result = req.Skip(index + 1).Take(length).ToArray();
            return result;
        }

        private static byte[] getHostAndType(byte[] req, ref int outIndex, int startIndex = 12)
        {
            int index = startIndex;
            var result = new List<byte>();
            while (req[index] != 0x00)
            {
                var length = req[index];
                if (length == 192) //значит это ссылка, первые два бита единицы
                {
                    var host = getHostAndType(req, ref outIndex, req[index + 1]);
                    host = host.Take(host.Length - 2).ToArray();
                    result.AddRange(host);
                    result.AddRange(Encoding.ASCII.GetBytes("."));
                    index += 1;
                    break;
                }
                else
                {
                    result.AddRange(getLabel(req, index));
                    result.AddRange(Encoding.ASCII.GetBytes("."));
                }
                index += length + 1;
            }
            //удаляем последнюю точку
            result = result.Take(result.Count - 1).ToList();
            //Добавляем тип
            result.Add(req[index + 1]);
            result.Add(req[index + 2]);
            outIndex = index + 5;//индекс начала записи ttl в ответе 
            return result.ToArray();
        }

        private static byte[] getPacket()
        {
            string host = hostName;
            byte[] hostnameLength = new byte[1];
            byte[] hostdomainLength = new byte[1];

            byte[] ID = { 0x46, 0x62 };
            byte[] queryType = { 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            byte[] hostname = System.Text.ASCIIEncoding.Default.GetBytes(host.Split('.')[0]);
            hostnameLength[0] = (byte)hostname.Length;
            byte[] hostdomain = System.Text.ASCIIEncoding.Default.GetBytes(host.Split('.')[1]);
            hostdomainLength[0] = (byte)hostdomain.Length;
            byte[] queryEnd = { 0x00, 0x00, type[1], 0x00, 0x01 };// type -0001(A), class - 0001
            byte[] dnsQueryString = ID.Concat(queryType).Concat(hostnameLength).Concat(hostname).Concat(hostdomainLength).Concat(hostdomain).Concat(queryEnd).ToArray();

            return dnsQueryString;
        }

        public static void MakeRequest()
        {
            var data = getPacket();

            UdpClient sender = new UdpClient(); // создаем UdpClient для отправки сообщений
            try
            {
                sender.Send(data, data.Length, ip, port); // отправка    
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
    }
}
