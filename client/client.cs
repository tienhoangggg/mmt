using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        public static TcpClient client = new TcpClient();
        public static NetworkStream stream;
        public static StreamReader reader;
        public static StreamWriter writer;
        private static void Main()
        {
            var ip = IPAddress.Parse("192.168.50.130");
            client.Connect(ip, 8888); //kết nối đến ip trên ở port 8888
            stream = client.GetStream();
            writer = new StreamWriter(stream) { AutoFlush = true };
            reader = new StreamReader(stream);
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            //gửi và nhận message đến server liên quan đến chức năng login và register
            //thoát khỏi vòng lặp khi server phản hồi đã đăng nhập thành công
            while (true)
            {
                Console.Clear();
                Console.WriteLine("login <name>");
                Console.WriteLine("register <new_name>");
                Console.Write(">");
                string name = Console.ReadLine();
                if (name.Length <= 6)
                    continue;
                if (name.Length > 6 && name.Length <= 9)
                    if (name.Substring(0, 6) != "login ")
                        continue;
                if (name.Substring(0, 6) != "login " && name.Substring(0, 9) != "register ")
                    continue;
                Console.Write(">>password: ");
                string pass = passwd();
                Console.Write("\nDo you want to encrypt message before sending? (y/n) (default is no) :");
                string c = Console.ReadLine();
                c = c.ToUpper();
                if (c == "Y" || c == "YES")
                {
                    name = encrypt(name);
                    pass = encrypt(pass);
                }
                writer.WriteLine(c);
                writer.WriteLine(name);
                writer.WriteLine(pass);
                c = reader.ReadLine();
                Console.WriteLine(c);
                Thread.Sleep(1000);
                if (c == "pass_true")
                    break;
            }
            //sau khi đăng nhập thành công sẽ bát đầu luồng mới ở hàm command
            Thread command = new Thread(Command);
            command.Start();
        }
        public static void Command()
        {
            menu_command();
            //gửi và nhận các message đến server liên quan đến các hàm start_game, change_password, create_room, chech_user, setup_info
            while (true)
            {
                if (receive.Length > 7)
                    if (receive.Substring(0, 7) == "battle ")
                    {
                        writer.WriteLine("close");
                        Console.Clear();
                        Console.WriteLine(receive.Substring(7) + " want fight with you, do you want?(y/n) (default is no) ");
                        while (keyboard == "") ;
                        keyboard = keyboard.ToUpper();
                        writer.WriteLine(keyboard);
                        if (keyboard == "Y" || keyboard == "YES")
                        {
                            Thread battle = new Thread(Battle);
                            battle.Start("2");
                            break;
                        }
                        menu_command();
                    }
                if (keyboard != "")
                {
                    writer.WriteLine(keyboard);
                    if (keyboard == "change_password")
                    {
                        Console.Write(">>current_password: ");
                        string pass = passwd();
                        Console.Write("\nDo you want to encrypt password before sending? (y/n) (default is no) :");
                        string c = Console.ReadLine();
                        c = c.ToUpper();
                        writer.WriteLine(c);
                        if (c == "Y" || c == "YES")
                            pass = encrypt(pass);
                        writer.WriteLine(pass);
                        while (receive == "") ;
                        pass = receive;
                        if (pass == "pass_false")
                            Console.WriteLine(pass);
                        else
                        {
                            Console.Write(">>new_password: ");
                            pass = passwd();
                            if (c == "Y" || c == "YES")
                                pass = encrypt(pass);
                            writer.WriteLine(pass);
                            Console.WriteLine("yours password is change");
                        }
                        Thread.Sleep(1000);
                        menu_command();
                        continue;
                    }
                    if (keyboard.Length > 12)
                        if (keyboard.Substring(0, 12) == "create_room ")
                        {
                            Console.WriteLine("wait...");
                            while (receive == "") ;
                            if (receive != "accept")
                            {
                                Console.WriteLine(receive);
                                Thread.Sleep(1000);
                                menu_command();
                                continue;
                            }
                            Thread battle = new Thread(Battle);
                            battle.Start("1");
                            battle.Join();
                            break;
                        }
                    if ((keyboard.Length < 10) || (keyboard.Substring(0, 10) != "start_game" && keyboard.Substring(0, 10) != "check_user" && keyboard.Substring(0, 10) != "setup_info"))
                    {
                        menu_command();
                        continue;
                    }
                    while (receive == "") ;
                    receive = receive.Replace('*', '\n');
                    Console.WriteLine(receive);
                    if (receive == "wrong syntax")
                        Thread.Sleep(1000);
                    else
                    {
                        Console.WriteLine("\n\n\npress any button to continue");
                        Console.ReadKey();
                    }
                    menu_command();
                }
            }
        }
        //hàm in ra hướng dẫn sử dụng câu lệnh cho người dùng
        public static void menu_command()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("start_game");
            Console.WriteLine("change_password");
            Console.WriteLine("create_room [id] with [name]");
            Console.WriteLine("check_user [-option] [username]");
            Console.WriteLine("[-option] : -find , -online , -show_date , -show_fullname , -show_note , -show_all , -show_point");
            Console.WriteLine("setup_info [-option] [information]");
            Console.WriteLine("[-option] : -fullname , -date , -note");
            Console.Write(">");
            receive = "";
            keyboard = "";
            Console.ForegroundColor = ConsoleColor.DarkRed;
            listen();
            KeyBoard();
        }
        public static string receive; //lưu message từ server
        public static string keyboard; //lưu message từ bàn phím người dùng
        //hàm nhận message từ bàn phím, hàm này chạy trong luồng song song với hàm command
        public static void KeyBoard()
        {
            new Task(() =>
            {
                keyboard = Console.ReadLine();
                if (keyboard == "")
                    keyboard = "no";
            }).Start();
        }
        //hàm nhận message từ server, hàm này chạy trong luồng song song với hàm command
        public static void listen()
        {
            new Task(() =>
            {
                receive = reader.ReadLine();
            }).Start();
        }
        //hàm này được gọi khi người dùng tham gia trận đấu
        //giá trị đầu vào là 1 hoặc 2, mô tả người dùng là người mời hay người được mời
        //khi hàm này kết thúc, người dùng sẽ trở lại hàm command
        public static void Battle(object obj)
        {
            int player = int.Parse(obj as string);
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.WriteLine("Rule: if you hit an enemy's ship, \"Hit\" will be displayed and you will gain an extra move,\nelse if you don't hit, \"Miss\" will be displayed and end your turn");
                Console.Write("enter path to the file text which have ship's position: ");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                string temp = Console.ReadLine();
                string[] map;
                try
                {
                    map = File.ReadAllLines(temp);
                }
                catch
                {
                    Console.WriteLine("wrong path");
                    Thread.Sleep(1000);
                    continue;
                };
                temp = "";
                for (int i = 0; i < map.Length; i++)
                    temp += map[i];
                writer.WriteLine(temp);
                Console.WriteLine("wait another player...");
                int score_u = 0, score_e = 0, point_ladder_u, point_ladder_e;
                if (!int.TryParse(reader.ReadLine(), out point_ladder_u))
                {
                    Console.WriteLine("enemy disconnect");
                    Thread.Sleep(1000);
                    break;
                }
                if (!int.TryParse(reader.ReadLine(), out point_ladder_e))
                {
                    Console.WriteLine("enemy disconnect");
                    Thread.Sleep(1000);
                    break;
                }
                Console.Clear();
                Console.WriteLine($"you : {score_u}/{point_ladder_u}    |   enemy : {score_e}/{point_ladder_e}");
                if (player == 1)
                {
                    do
                    {
                        Console.Write("your turn: ");
                        string locate = Console.ReadLine();
                        string[] row_col = locate.Split(' ');
                        if (row_col.Length != 2 || !int.TryParse(row_col[0], out int row) || !int.TryParse(row_col[1], out int col))
                        {
                            Console.WriteLine("wrong syntax");
                            Thread.Sleep(1000);
                            Console.Clear();
                            Console.WriteLine($"you : {score_u}/{point_ladder_u}    |   enemy : {score_e}/{point_ladder_e}");
                            continue;
                        }
                        writer.WriteLine(locate);
                        break;
                    } while (true);
                }
                else
                    Console.WriteLine("enemy's turn..");
                while (true)
                {
                    temp = reader.ReadLine();
                    if (temp == "sorry, room will close")
                    {
                        Console.WriteLine("enemy disconnect");
                        Thread.Sleep(1000);
                        goto Flag;
                    }
                    Console.Clear();
                    if (temp == "Hit")
                        score_u++;
                    if (temp == "enemy has hit your ship")
                        score_e++;
                    Console.WriteLine($"you : {score_u}/{point_ladder_u}    |   enemy : {score_e}/{point_ladder_e}");
                    Console.WriteLine(temp);
                    Thread.Sleep(1000);
                    if (temp == "you win" || temp == "you lose")
                        break;
                    Console.Clear();
                    Console.WriteLine($"you : {score_u}/{point_ladder_u}    |   enemy : {score_e}/{point_ladder_e}");
                    if (temp == "Hit" || temp == "enemy missed")
                    {
                        do
                        {
                            Console.Write("your turn: ");
                            string locate = Console.ReadLine();
                            string[] row_col = locate.Split(' ');
                            if (row_col.Length != 2 || !int.TryParse(row_col[0], out int row) || !int.TryParse(row_col[1], out int col))
                            {
                                Console.WriteLine("wrong syntax");
                                Thread.Sleep(1000);
                                Console.Clear();
                                Console.WriteLine($"you : {score_u}/{point_ladder_u}    |   enemy : {score_e}/{point_ladder_e}");
                                continue;
                            }
                            writer.WriteLine(locate);
                            break;
                        } while (true);
                    }
                    else
                        Console.WriteLine("enemy's turn..");
                }
                Console.Write("do you want play again with him? (y/n) (default is no)");
                temp = Console.ReadLine();
                temp = temp.ToUpper();
                writer.WriteLine(temp);
                temp = reader.ReadLine();
                Console.WriteLine(temp);
                Thread.Sleep(1500);
                if (temp != "continue")
                    break;
            }
        Flag:;
            Thread command = new Thread(Command);
            command.Start();
        }
        public static string passwd()
        {
            string temp = "";
            ConsoleKeyInfo info = Console.ReadKey(true);
            while (info.Key != ConsoleKey.Enter)
            {
                if (info.Key != ConsoleKey.Backspace)
                    temp += info.KeyChar;
                else if (temp.Length > 0)
                    temp = temp.Substring(0, temp.Length - 1);
                info = Console.ReadKey(true);
            }
            return temp;
        }
        public static string encrypt(string s)
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
    }
}