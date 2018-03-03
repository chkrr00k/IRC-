
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;

//made by chkrr00k;

namespace IRCclient {

    class Connection {
        private TcpClient sock;
        private bool isConnected;
        private StreamReader strmRd;
        private StreamWriter strmWr;

        public Connection(string address, short port) {
            this.isConnected = false;
            this.sock = new TcpClient(address, port);
            if(this.sock.Connected) {
                this.isConnected = true;
            } else {
                throw new IOException();
            }
            this.strmRd = new StreamReader(this.sock.GetStream());
            this.strmWr = new StreamWriter(this.sock.GetStream());
        }

        public string readLine() {
            if(this.isConnected) {
                return this.strmRd.ReadLine();
            } else {
                throw new IOException();
            }
        }

        public void writeLine(string input) {
            if(this.isConnected) {
                this.strmWr.WriteLine(input);
                this.strmWr.Flush();
            } else {
                throw new IOException();
            }
        }
        public void close() {
            if(this.isConnected) {
                this.strmWr.Close();
                this.strmRd.Close();
                this.sock.Close();
            }
        }
    }

    abstract class Message {
        public string sender { get; set; }
        public string message { get; set; }
        public string channel { get; set; }

        public static Message parse(string input) {
            if(ChannelMessage.check(input)) {
                return ChannelMessage.of(input);
            }else if(StatusMessage.check(input)) {
                return StatusMessage.of(input);
            }else if(PrivateMessage.check(input)) {
                return PrivateMessage.of(input);
            }else {
                return UnknownMessage.of(input);
            }
        }

    }

    class ChannelMessage: Message {

        public static Regex msgRe = new Regex(":(.+)\\!.+ PRIVMSG (\\#[^\\s]+) \\:(.+)", RegexOptions.IgnoreCase);

        public ChannelMessage(string input) : base() {
            MatchCollection mtch = ChannelMessage.msgRe.Matches(input);
            if(mtch.Count > 0 && mtch[0].Groups.Count == 4) {
                base.sender = mtch[0].Groups[1].Value;
                base.message = mtch[0].Groups[3].Value;
                base.channel = mtch[0].Groups[2].Value;
            }
        }

        internal static bool check(string input) {
            return ChannelMessage.msgRe.IsMatch(input);
        }

        internal static Message of(string input) {
            return new ChannelMessage(input);
        }

        public override string ToString() {
            return "<" + base.sender + "@" + base.channel + "> " + base.message;
        }
    }

    class PrivateMessage: Message {

        public static Regex msgRe = new Regex(":(.+)\\!.+ PRIVMSG ([^\\#][^\\s]+) \\:(.+)", RegexOptions.IgnoreCase);

        public PrivateMessage(string input) : base() {
            MatchCollection mtch = PrivateMessage.msgRe.Matches(input);
            if(mtch.Count > 0 && mtch[0].Groups.Count == 4) {
                base.sender = mtch[0].Groups[1].Value;
                base.message = mtch[0].Groups[3].Value;
                base.channel = mtch[0].Groups[2].Value;
            }
        }

        internal static bool check(string input) {
            return PrivateMessage.msgRe.IsMatch(input);
        }

        internal static Message of(string input) {
            return new PrivateMessage(input);
        }

        public override string ToString() {
            return "<" + base.sender + "@" + base.channel + "> " + base.message;
        }
    }

    class StatusMessage: Message {
        public static Regex topic = new Regex(@":(.+)\!.+ TOPIC (\#\w+) \:(.+)", RegexOptions.IgnoreCase);
        public static Regex mode = new Regex(@":(.+)\!.+ MODE (\S+) :?(\+|\-)(\S+)(\s(.+))?", RegexOptions.IgnoreCase);
        public static Regex nick = new Regex(@":(.+)\!.+ NICK (.+)", RegexOptions.IgnoreCase);
        public static Regex part = new Regex(@":(.+)\!.+ PART (\#\w+)( \:(.+))?", RegexOptions.IgnoreCase);
        public static Regex quit = new Regex(@":(.+)\!.+ QUIT( \:(.+))?", RegexOptions.IgnoreCase);
        public static Regex join = new Regex(@":(.+)\!.+ JOIN \:(\#\w+)", RegexOptions.IgnoreCase);
        public static Regex kick = new Regex(@":(.+)\!.+ KICK (\#\w+)([^\#]\w+)( \:(.+))?", RegexOptions.IgnoreCase);
        public static Regex generic = new Regex(@":(\S*)\s(\d*)\s(\w+)\s(.*)", RegexOptions.IgnoreCase);
        public static Regex notice = new Regex(@":(.+)\!.+ NOTICE ([^\#]\w+) \:(.+)", RegexOptions.IgnoreCase);

        private string status;

        public StatusMessage(string status) {
            this.status = status;
        }

        public override string ToString() {
            return this.status;
        }

        public static StatusMessage of(string input) {
            GroupCollection tmp;
            if(StatusMessage.topic.IsMatch(input)) {
                tmp = StatusMessage.topic.Matches(input)[0].Groups;
                return new StatusMessage("" + tmp[1] + " has changed the topic of " + tmp[2] + " to " + tmp[3] + "");
            } else if(StatusMessage.mode.IsMatch(input)) {
                tmp = StatusMessage.mode.Matches(input)[0].Groups;
                return new StatusMessage("" + tmp[1] + " has changed the mode " + tmp[2] + " to " + tmp[3] + "");
            } else if(StatusMessage.nick.IsMatch(input)) {
                tmp = StatusMessage.nick.Matches(input)[0].Groups;
                return new StatusMessage("" + tmp[1] + " has changed the topic of " + tmp[2] + " to " + tmp[3] + "");
            } else if(StatusMessage.part.IsMatch(input)) {
                tmp = StatusMessage.part.Matches(input)[0].Groups;
                return new StatusMessage("" + tmp[1] + " has changed the topic of " + tmp[2] + " to " + tmp[3] + "");
            } else if(StatusMessage.quit.IsMatch(input)) {
                tmp = StatusMessage.quit.Matches(input)[0].Groups;
                return new StatusMessage("" + tmp[1] + " has quitted " + (tmp.Count > 1 ? "  " + tmp[2] + "" : ""));
            } else if(StatusMessage.join.IsMatch(input)) {
                tmp = StatusMessage.join.Matches(input)[0].Groups;
                return new StatusMessage("" + tmp[1] + " has joined " + tmp[2] + "");
            } else if(StatusMessage.kick.IsMatch(input)) {
                tmp = StatusMessage.kick.Matches(input)[0].Groups;
                return new StatusMessage("" + tmp[1] + " was kicked from " + tmp[2] + (tmp.Count > 2 ?"  " + tmp[3] + "" :""));
            } else if(StatusMessage.generic.IsMatch(input)) {
                tmp = StatusMessage.generic.Matches(input)[0].Groups;
                return new StatusMessage("" + tmp[1] + " " + tmp[2] + " " + tmp[3] + " " + tmp[4] + "");
            } else if(StatusMessage.notice.IsMatch(input)) {
                tmp = StatusMessage.notice.Matches(input)[0].Groups;
                return new StatusMessage("[" + tmp[1] + "@" + tmp[2] + "] " + tmp[3] + "");
            } else {
                return new StatusMessage(input);
            }
        }

        internal static bool check(string input) {
            return (StatusMessage.topic.IsMatch(input))
                || (StatusMessage.mode.IsMatch(input))
                || (StatusMessage.nick.IsMatch(input))
                || (StatusMessage.part.IsMatch(input))
                || (StatusMessage.quit.IsMatch(input))
                || (StatusMessage.join.IsMatch(input))
                || (StatusMessage.kick.IsMatch(input))
                || (StatusMessage.generic.IsMatch(input))
                || (StatusMessage.notice.IsMatch(input));
        }
    }

    class UnknownMessage : Message {
        private new string message;

        public UnknownMessage(string message) {
            this.message = message;
        }

        public override string ToString() {
            return this.message;
        }

        public static Message of(string input) {
            return new UnknownMessage(input);
        }
    }

    class Channel {
        private List<Message> messages;
        private string name;

        public Channel(string name) {
            this.name = name;
            this.messages = new List<Message>();
        }

        public List<Message> getMessages() {
            return this.messages;
        }

        public void addMessage(Message input) {
            this.messages.Add(input);
        }

        public void addMessage(string input) {
            this.messages.Add(new ChannelMessage(input));
        }
    }

    class IRC {
        private Dictionary<string, Channel> channels;
        private Connection conn;
        private List<string> chanToJoin;
        private int number;
        private Thread tr;


        public IRC(string host, short port, string nick = "CID", string realname = "realname", string hostn = "host", string ident = "ident") {
            this.channels = new Dictionary<string, Channel>();
            this.conn = new Connection(host, port);
            this.number = 0;
            this.chanToJoin = new List<string>();
            conn.writeLine("NICK " + nick + "\r\n");
            conn.writeLine("USER " + ident + " " + hostn + " ayy :" + realname + "\r\n");
            this.tr = new Thread(new ThreadStart(this.parse));
            this.tr.Start();
        }

        public void addChannel(string chanName) {
            if(this.number < 6) {
                this.chanToJoin.Add(chanName);
            } else {
                this.joinChannel(chanName);
            }
        }

        private void joinChannel(string chanName) {
            if(chanName.StartsWith("#")) {
                conn.writeLine("JOIN " + chanName + "\r\n");
                this.channels.Add(chanName, new Channel(chanName));
            } else {
                throw new InvalidDataException();
            }
        }

        public void parse() {
            string tmp = "";
            Message msg = null;
            bool auth = false;
            for(;;) {
                tmp = conn.readLine().Trim();
                if(!auth) {
                    if(this.number < 6) {
                        this.number++;
                    } else if(this.number == 6) {
                        this.chanToJoin.ForEach(x => this.joinChannel(x));
                        this.chanToJoin.Clear();
                        auth = true;
                    }
                }
                if(tmp.StartsWith("PING ")) {
                    conn.writeLine("PONG " + tmp.Split()[1] + "\r\n");
                }else {
                    msg = Message.parse(tmp);
                    if(msg is ChannelMessage) {
                        this.channels[msg.channel].addMessage(msg);
                    }
                    Console.WriteLine(msg);
                }
            }
        }

        public void sendMessage(string channel, string message) {
            this.conn.writeLine("PRIVMSG " + channel + " :" + message + "\r\n");
        }

        public void disconnect() {
            this.conn.writeLine("QUIT :developing an irc client is hard af\r\n");
        }
    }

    class Program {

        static void Main(string[] args) {
            List<string> chans = new List<string>();
            string tmp = "";
            Console.WriteLine("Write the channels you want to join, enter after each chan, write start (all lowercase) to start the client");
            while((tmp = Console.ReadLine()) != null && !tmp.Equals("start")) {
                if(tmp.StartsWith("#") && !tmp.Contains(" ")) {
                    chans.Add(tmp.Trim());
                } else {
                    Console.WriteLine("Channels starts with \"#\"!");
                }
            }
            IRC irc = new IRC("irc.rizon.net", 6667);
            chans.ForEach(x => irc.addChannel(x));

            Console.CancelKeyPress += delegate {
                irc.disconnect();
            };
            while((tmp = Console.ReadLine()) != null) {
                if(tmp.StartsWith("/")) {
                    tmp = tmp.Substring(1);
                    Console.WriteLine(tmp);
                    switch(tmp.Split(' ')[0]) {
                        case "nick":
                            break;
                        case "join":
                            if(tmp.Length > 5 && !tmp.Substring(5).Contains(' ') && tmp.Split(' ').Length == 2) {
                                irc.addChannel(tmp.Split(' ')[1]);
                            }else {
                                Console.WriteLine("Wrong syntax");
                            }
                            break;
                        case "part":
                            break;
                        default:
                            break;

                    }
                    
                } else {
                    irc.sendMessage(tmp.Split(' ')[0], string.Join(" ", tmp.Split(' ').Skip(1)));
                }
            }

        }
    }
}
