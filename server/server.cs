using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Server
{
    //cấu trúc thông tin của mỗi người dùng, thông tin này sẽ được lưu trong database
    public class player
    {
        internal TcpClient online = null;
        public string user { get; set; }
        public string password { get; set; }
        public string date { get; set; }
        public string fullname { get; set; }
        public string note { get; set; }
        public int point { get; set; }
    }
    //cấu trúc lưu thông tin của mỗi phòng chơi, đó là tên của 2 người đang nằm trong phòng chơi
    //player1 lưu tên của thằng mời, player2 lưu tên của thằng được player1 mời
    public struct room_info
    {
        public string player1;
        public string player2;
    }
    public class server
    {
        // hàm giải mã, giải mã đoạn text đã bị mã hóa được gửi từ client
        public static string decrypt(string s)
        {
            string new_s = "";
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] >= '0' && s[i] <= '9')
                    new_s += (char)('0' + (int)(s[i] - '0' + 5) % 10);
                if (s[i] >= 'a' && s[i] <= 'z')
                    new_s += (char)('a' + (int)(s[i] - 'a' + 13) % 26);
                if (s[i] >= 'A' && s[i] <= 'Z')
                    new_s += (char)('A' + (int)(s[i] - 'A' + 13) % 26);
                if (s[i] == ' ')
                    new_s += ' ';
            }
            return new_s;
        }
        //list là mảng lưu thông tin của toàn bộ user, mỗi khi server được bật, toàn bộ thông tin trong file database sẽ được đọc và lưu vào mảng này
        public static Dictionary<string, player> list = new Dictionary<string, player>();
        const int MAX = 1000; //số lượng room tối đa
        public static room_info[] room;
        public static void Main()
        {
            //3 dòng code dưới sẽ đọc thông tin trong database vào mảng list
            StreamReader f_in = new StreamReader("database.json");
            list = JsonConvert.DeserializeObject<Dictionary<string, player>>(f_in.ReadToEnd());
            f_in.Close();
            TcpListener listener = new TcpListener(IPAddress.Any, 8888); //server lấy port 8888 và sử dụng giao thức tcp
            room = new room_info[MAX]; //tạo MAX phòng trống
            for (int i = 0; i < MAX; i++)
                room[i].player1 = room[i].player2 = "";
            //mỗi vòng lặp, server sẽ lắng nghe và khi có client sẽ accept kết nối và tạo luồng cho client chạy hàm login_register
            while (true)
            {
                listener.Start();
                TcpClient client = new TcpClient();
                client = listener.AcceptTcpClient();
                Thread new_client = new Thread(login_register);
                new_client.Start(client);
            }
        }
        public static void login_register(object obj)
        {
            //4 dòng code dưới sử dụng thư viện có sẵn nhằm đơn giản hóa việc code trao đổi dữ liệu giữa server và client
            //reader có thể nhận message từ client, writer có thể gửi message đến client
            TcpClient client = obj as TcpClient;
            var stream = client.GetStream();
            var reader = new StreamReader(stream);
            var writer = new StreamWriter(stream) { AutoFlush = true };
            string cur = ""; //khi client đăng nhập thành công, cur sẽ lưu tên của tài khoản được đăng nhập
            try
            {
                //xử lí liên tục các request nhận được từ client
                //khi client đăng nhập thành công sẽ lưu cur = name của tài khoản được đăng nhập và break khỏi vòng lặp
                while (true)
                {
                    string c = reader.ReadLine();
                    string name = reader.ReadLine();
                    string pass = reader.ReadLine();
                    if (c == "Y" || c == "YES")
                    {
                        name = decrypt(name);
                        pass = decrypt(pass);
                    }
                    if (name.Length > 9)
                        if (name.Substring(0, 9) == "register ")
                        {
                            if (list.ContainsKey(name.Substring(9)))
                            {
                                writer.WriteLine("name already exists");
                                continue;
                            }
                            player new_player = new player();
                            new_player.password = pass;
                            new_player.user = name.Substring(9);
                            list.Add(new_player.user, new_player);
                            writer.WriteLine("success");
                            update_data();
                        }
                    if (name.Length > 6)
                        if (name.Substring(0, 6) == "login ")
                        {
                            if (!list.ContainsKey(name.Substring(6)))
                            {
                                writer.WriteLine("name does not exist");
                                continue;
                            }
                            if (list[name.Substring(6)].password == pass)
                            {
                                cur = list[name.Substring(6)].user;
                                writer.WriteLine("pass_true");
                            }
                            else
                                writer.WriteLine("pass_false");
                            if (cur != "")
                                break;
                        }
                }
                list[cur].online = client; //lưu đường kết nối hiện tại vào thông tin tài khoản, khi tài khoản không được đăng nhập, giá trị này = null
                //sau khi đăng nhập thành công, luồng hiện tại sẽ kết thúc và chuyển người dùng sang luồng mới ở hàm command, giá trị đầu vào của hàm command là name của người dùng
                Thread command = new Thread(Command);
                command.Start(cur);
            }
            catch
            {
                //trong trường hợp client ngắt kết nối, luồng này sẽ kết thúc
                if (cur != "")
                    list[cur].online = null;
            }
        }
        public static void Command(object obj)
        {
            string cur = obj as string;
            try
            {
                //4 dòng code dưới sử dụng thư viện có sẵn nhằm đơn giản hóa việc code trao đổi dữ liệu giữa server và client
                //reader có thể nhận messege từ client, writer có thể gửi messge đến client
                TcpClient client = list[cur].online;
                var stream = client.GetStream();
                var reader = new StreamReader(stream);
                var writer = new StreamWriter(stream) { AutoFlush = true };
                //nhận và xử li liên tục các message nhận được từ người dùng
                while (true)
                {
                    string s = reader.ReadLine();
                    if (s == "change_password")
                    {
                        string c = reader.ReadLine();
                        string temp = reader.ReadLine();
                        if (c == "Y" || c == "YES")
                            temp = decrypt(temp);
                        if (list[cur].password == temp)
                        {
                            writer.WriteLine("pass_true");
                            temp = reader.ReadLine();
                            if (c == "Y" || c == "YES")
                                temp = decrypt(temp);
                            list[cur].password = temp;
                            update_data();
                        }
                        else
                            writer.WriteLine("pass_false");
                        continue;
                    }
                    if (s == "start_game")
                    {
                        string temp = "List users is online: ";
                        foreach (var item in list)
                        {
                            if (item.Value.online != null)
                                if (item.Key != cur)
                                    temp += item.Key + " , ";
                        }
                        writer.WriteLine(temp);
                        continue;
                    }
                    if (s == "close")
                        break;
                    string[] ss = s.Split(' ');
                    if (ss[0] == "create_room")
                    {
                        if (ss.Length != 4)
                        {
                            writer.WriteLine("wrong syntax");
                            continue;
                        }
                        int id;
                        if (int.TryParse(ss[1], out id) == false)
                        {
                            writer.WriteLine("id was wrong");
                            continue;
                        }
                        if (id < 0 || id >= MAX)
                        {
                            writer.WriteLine($"id between 0 and {MAX - 1}");
                            continue;
                        }
                        if (!list.ContainsKey(ss[3]))
                        {
                            writer.WriteLine("your competitor doesn't exist");
                            continue;
                        }
                        if (list[ss[3]].online == null)
                        {
                            writer.WriteLine($"{ss[3]} does not online");
                            continue;
                        }
                        if (room[id].player1 != "")
                        {
                            writer.WriteLine("The room is occupied");
                            continue;
                        }
                        room[id].player1 = cur;
                        room[id].player2 = ss[3];
                        Thread game = new Thread(battle);
                        game.Start(Convert.ToString(id));
                        break;
                    }
                    if (ss[0] == "check_user")
                    {
                        if (!list.ContainsKey(ss[2]))
                        {
                            writer.WriteLine("name does not exist");
                            continue;
                        }
                        if (ss[1] == "-find")
                        {
                            writer.WriteLine("name is exist");
                            continue;
                        }
                        if (ss[1] == "-online")
                        {
                            if (list[ss[2]].online != null)
                                writer.WriteLine("the player is online");
                            else
                                writer.WriteLine("the player is not online");
                            continue;
                        }
                        if (ss[1] == "-show_date")
                        {
                            if (list[ss[2]].date == null)
                                writer.WriteLine("the player has not written date");
                            else
                                writer.WriteLine(list[ss[2]].date);
                            continue;
                        }
                        if (ss[1] == "-show_fullname")
                        {
                            if (list[ss[2]].fullname == null)
                                writer.WriteLine("the player has not written full name");
                            else
                                writer.WriteLine(list[ss[2]].fullname);
                            continue;
                        }
                        if (ss[1] == "-show_note")
                        {
                            if (list[ss[2]].note == null)
                                writer.WriteLine("the player has not written note");
                            else
                                writer.WriteLine(list[ss[2]].note);
                            continue;
                        }
                        if (ss[1] == "-show_point")
                        {
                            writer.WriteLine(list[ss[2]].point);
                            continue;
                        }
                        if (ss[1] == "-show_all")
                        {
                            string sss = "";
                            if (list[ss[2]].fullname == null)
                                sss += "the player has not written full name*";
                            else
                                sss += list[ss[2]].fullname + '*';
                            if (list[ss[2]].date == null)
                                sss += "the player has not written date*";
                            else
                                sss += list[ss[2]].date + '*';
                            if (list[ss[2]].online != null)
                                sss += "the player is online*";
                            else
                                sss += "the player is not online*";
                            if (list[ss[2]].note == null)
                                sss += "the player has not written note*";
                            else
                                sss += list[ss[2]].note + '*';
                            sss += list[ss[2]].point.ToString();
                            writer.WriteLine(sss);
                            continue;
                        }
                        writer.WriteLine("wrong syntax");
                    }
                    if (ss[0] == "setup_info")
                    {
                        if (ss[1] == "-date")
                        {
                            list[cur].date = ss[2];
                            writer.WriteLine("birthday of " + list[cur].user + " is " + list[cur].date);
                            update_data();
                            continue;
                        }
                        string[] sss = s.Split('"');
                        if (sss.Length != 3)
                        {
                            writer.WriteLine("wrong syntax");
                            continue;
                        }
                        if (ss[1] == "-fullname")
                        {
                            list[cur].fullname = sss[1];
                            writer.WriteLine("name of " + list[cur].user + " is \"" + list[cur].fullname + "\"");
                            update_data();
                            continue;
                        }
                        if (ss[1] == "-note")
                        {
                            list[cur].note = sss[1];
                            writer.WriteLine(list[cur].user + "'s note: " + list[cur].note);
                            update_data();
                            continue;
                        }
                        writer.WriteLine("wrong syntax");
                    }
                }
            }
            catch
            {
                //trong trường hợp client ngắt kết nối, luồng này sẽ kết thúc
                list[cur].online = null;
            }
        }
        //cập nhật thông tin từ mảng list vào database, hàm này được gọi mỗi khi thông tin có sự thay đổi như register, change_password, setup_info
        public static void update_data()
        {
            StreamWriter f_out = new StreamWriter("database.json");
            f_out.Write(JsonConvert.SerializeObject(list, Formatting.Indented));
            f_out.Close();
        }
        //hàm này được gọi khi có người dùng gửi lời mời đến người dùng khác, đầu vào của hàm là id room chứa tên của 2 người chơi
        //khi hàm này kết thúc, room sẽ được làm rỗng và 2 người chơi sẽ được tạo luồng mới ở hàm command
        //hàm kết thúc khi player ngừng chơi hoặc mất kết nối
        public static void battle(object obj)
        {
            int id = int.Parse(obj as string);
            string name1 = room[id].player1;
            string name2 = room[id].player2;
            var stream1 = list[name1].online.GetStream();
            var stream2 = list[name2].online.GetStream();
            var reader1 = new StreamReader(stream1);
            var reader2 = new StreamReader(stream2);
            var writer1 = new StreamWriter(stream1) { AutoFlush = true };
            var writer2 = new StreamWriter(stream2) { AutoFlush = true };
            writer2.WriteLine("battle " + name1);
            Thread.Sleep(200);
            string c = reader2.ReadLine();
            Thread command1 = new Thread(Command);
            Thread command2 = new Thread(Command);
            if (c != "Y" && c != "YES")
            {
                writer1.WriteLine(name2 + " does not accept");
                room[id].player1 = "";
                room[id].player2 = "";
                command1.Start(name1);
                command2.Start(name2);
                return;
            }
            writer1.WriteLine("accept");
            try
            {
                while (true)
                {
                    string temp = reader1.ReadLine();
                    bool[,] map1 = new bool[20, 20];
                    int count1 = 0, count2 = 0;
                    for (int i = 0; i < 400; i++)
                    {
                        if (temp[i] == '*')
                        { map1[i / 20, i % 20] = true; count1++; }
                        else
                            map1[i / 20, i % 20] = false;
                    }
                    temp = reader2.ReadLine();
                    bool[,] map2 = new bool[20, 20];
                    for (int i = 0; i < 400; i++)
                    {
                        if (temp[i] == '*')
                        { map2[i / 20, i % 20] = true; count2++; }
                        else
                            map2[i / 20, i % 20] = false;
                    }
                    writer1.WriteLine(Convert.ToString(count2));
                    writer1.WriteLine(Convert.ToString(count1));
                    writer2.WriteLine(Convert.ToString(count1));
                    writer2.WriteLine(Convert.ToString(count2));
                    bool turn = true;
                    string[] locate = new string[2];
                    int row, col;
                    while (true)
                    {
                        if (turn)
                        {
                            temp = reader1.ReadLine();
                            locate = temp.Split(' ');
                            row = int.Parse(locate[0]);
                            col = int.Parse(locate[1]);
                            if (map2[row, col])
                            {
                                map2[row, col] = false;
                                count2--;
                                if (count2 == 0)
                                {
                                    list[name1].point++;
                                    writer2.WriteLine("you lose");
                                    writer1.WriteLine("you win");
                                    break;
                                }
                                else
                                {
                                    writer1.WriteLine("Hit");
                                    writer2.WriteLine("enemy has hit your ship");
                                }
                            }
                            else
                            {
                                writer1.WriteLine("Miss");
                                writer2.WriteLine("enemy missed");
                                turn = !turn;
                            }
                        }
                        else
                        {
                            temp = reader2.ReadLine();
                            locate = temp.Split(' ');
                            row = int.Parse(locate[0]);
                            col = int.Parse(locate[1]);
                            if (map1[row, col])
                            {
                                map1[row, col] = false;
                                count1--;
                                if (count1 == 0)
                                {
                                    list[name2].point++;
                                    writer1.WriteLine("you lose");
                                    writer2.WriteLine("you win");
                                    break;
                                }
                                else
                                {
                                    writer2.WriteLine("Hit");
                                    writer1.WriteLine("enemy has hit your ship");
                                }
                            }
                            else
                            {
                                writer2.WriteLine("Miss");
                                writer1.WriteLine("enemy missed");
                                turn = !turn;
                            }
                        }
                        update_data();
                    }
                    string temp1 = reader1.ReadLine();
                    string temp2 = reader2.ReadLine();
                    if ((temp1 == "Y" || temp1 == "YES") && (temp2 == "Y" || temp2 == "YES"))
                    {
                        writer1.WriteLine("continue");
                        writer2.WriteLine("continue");
                    }
                    else
                        break;
                }
            }
            catch { };
            try
            {
                writer1.WriteLine("sorry, room will close");
            }
            catch { };
            try
            {
                writer2.WriteLine("sorry, room will close");
            }
            catch { };
            room[id].player1 = "";
            room[id].player2 = "";
            command1.Start(name1);
            command2.Start(name2);
        }
    }
}