using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    public partial class Server : Form
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 5000);
        TcpClient client;
        Dictionary<string, TcpClient> clientList = new Dictionary<string, TcpClient>();
        CancellationTokenSource cancellation = new CancellationTokenSource();
        List<string> chat = new List<string>();
        IPBanManager ipBanManager = new IPBanManager();
        BadWordFilter badWordFilter = new BadWordFilter();
        Dictionary<string, int> userBadWordCount = new Dictionary<string, int>();

        public Server()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            cancellation = new CancellationTokenSource();
            startServer();
        }

        public void updateUI(String m)
        {
            this.Invoke((MethodInvoker)delegate
            {
                textBox1.AppendText(">>" + m + Environment.NewLine);
            });
        }

        public async void startServer()
        {
            listener.Start();
            updateUI("Sunucu başlatıldı.");
            updateUI("Kullanıcılar bekleniyor...");

            try
            {
                while (true)
                {
                    client = await Task.Run(() => listener.AcceptTcpClientAsync(), cancellation.Token);
                    string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                    if (ipBanManager.IsBanned(clientIP))
                    {
                        client.Close();
                        continue;
                    }

                    byte[] name = new byte[50];
                    NetworkStream stre = client.GetStream();
                    stre.Read(name, 0, name.Length);
                    String username = Encoding.ASCII.GetString(name).Split('$')[0];

                    lock (clientList)
                    {
                        clientList.Add(username, client);
                    }

                    this.Invoke((MethodInvoker)delegate
                    {
                        listBox1.Items.Add(username);
                    });

                    updateUI("Kullanıcı bağlandı: " + username + " - " + client.Client.RemoteEndPoint);
                    announce(username + " Katıldı ", username, false);
                    userBadWordCount[username] = 0;

                    await Task.Delay(1000).ContinueWith(t => sendUsersList());

                    var c = new Thread(() => ServerReceive(client, username));
                    c.Start();
                }
            }
            catch (Exception)
            {
                listener.Stop();
            }
        }

        public void announce(string msg, string uName, bool flag)
        {
            try
            {
                string badWordDetected = null;

                if (badWordFilter.ContainsBadWord(msg))
                {
                    badWordDetected = badWordFilter.badWords.FirstOrDefault(word => msg.ToLower().Contains(word.ToLower()));
                    msg = "Küfürlü kelime içeriyor ve engellendi.";

                    if (userBadWordCount.ContainsKey(uName))
                        userBadWordCount[uName]++;
                    else
                        userBadWordCount[uName] = 1;

                    // Yasaklı kelime admin paneline yazılıyor
                    updateUI($"Yasaklı Kelime Tespit Edildi: Kullanıcı: {uName}, Kelime: {badWordDetected}");

                    if (userBadWordCount[uName] >= 3)
                    {
                        TcpClient bannedClient = clientList.FirstOrDefault(x => x.Key == uName).Value;
                        if (bannedClient != null)
                        {
                            string clientIP = ((IPEndPoint)bannedClient.Client.RemoteEndPoint).Address.ToString();
                            ipBanManager.BanIP(clientIP, uName, badWordDetected);
                            updateUI($"Yasaklandı: {uName} - IP: {clientIP} - Kelime: {badWordDetected}");
                            bannedClient.Close();
                        }
                    }
                }

                foreach (var Item in clientList)
                {
                    TcpClient broadcastSocket = Item.Value;
                    NetworkStream broadcastStream = broadcastSocket.GetStream();
                    Byte[] broadcastBytes = null;

                    if (flag)
                    {
                        chat.Add("gChat");
                        chat.Add(uName + " : " + msg);
                        broadcastBytes = ObjectToByteArray(chat);
                    }
                    else
                    {
                        chat.Add("gChat");
                        chat.Add(msg);
                        broadcastBytes = ObjectToByteArray(chat);
                    }

                    broadcastStream.Write(broadcastBytes, 0, broadcastBytes.Length);
                    broadcastStream.Flush();
                    chat.Clear();
                }
            }
            catch (Exception) { }
        }

        public void ServerReceive(TcpClient clientn, String username)
        {
            byte[] data = new byte[1000];
            while (true)
            {
                try
                {
                    NetworkStream stream = clientn.GetStream();
                    stream.Read(data, 0, data.Length);
                    List<string> parts = (List<string>)ByteArrayToObject(data);

                    switch (parts[0])
                    {
                        case "gChat":
                            announce(parts[1], username, true);
                            break;
                    }
                }
                catch (Exception)
                {
                    updateUI("Kullanıcının Bağlantısı Kesildi: " + username);
                    lock (clientList)
                    {
                        clientList.Remove(username);
                    }
                    this.Invoke((MethodInvoker)delegate
                    {
                        listBox1.Items.Remove(username);
                    });
                    sendUsersList();
                    break;
                }
            }
        }

        private void btnServerStop_Click(object sender, EventArgs e)
        {
            listener.Stop();
            updateUI("Server Durduruldu");
        }

        public void sendUsersList()
        {
            try
            {
                byte[] userList = new byte[1024];
                string[] clist = listBox1.Items.OfType<string>().ToArray();
                List<string> users = new List<string> { "userList" };
                users.AddRange(clist);
                userList = ObjectToByteArray(users);

                foreach (var Item in clientList)
                {
                    TcpClient broadcastSocket = Item.Value;
                    NetworkStream broadcastStream = broadcastSocket.GetStream();
                    broadcastStream.Write(userList, 0, userList.Length);
                    broadcastStream.Flush();
                }
            }
            catch (Exception) { }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            textBox1.SelectionStart = textBox1.TextLength;
            textBox1.ScrollToCaret();
        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                string clientName = listBox1.GetItemText(listBox1.SelectedItem);

                if (clientList.ContainsKey(clientName))
                {
                    TcpClient clientSocket = clientList[clientName];
                    clientSocket.Close();
                    clientList.Remove(clientName);
                    updateUI($"Kullanıcı bağlantısı kesildi: {clientName}");

                    this.Invoke((MethodInvoker)delegate
                    {
                        listBox1.Items.Remove(clientName);
                    });

                    sendUsersList();
                }
            }
        }

        private void Private_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                string clientName = listBox1.GetItemText(listBox1.SelectedItem);

                if (clientList.ContainsKey(clientName))
                {
                    List<string> message = new List<string> { "private", "Admin", inputPrivate.Text };
                    TcpClient clientSocket = clientList[clientName];

                    NetworkStream stream = clientSocket.GetStream();
                    byte[] messageBytes = ObjectToByteArray(message);
                    stream.Write(messageBytes, 0, messageBytes.Length);
                    stream.Flush();

                    updateUI($"Özel mesaj gönderildi: {clientName} -> {inputPrivate.Text}");
                }
            }
        }

        public class IPBanManager
        {
            private HashSet<string> bannedIPs = new HashSet<string>();
            private string banFilePath = "banned_ips.txt";

            public IPBanManager()
            {
                LoadBannedIPs();
            }

            public void BanIP(string ipAddress, string username, string badWord)
            {
                if (!bannedIPs.Contains(ipAddress))
                {
                    bannedIPs.Add(ipAddress);
                    File.AppendAllText(banFilePath, $"{ipAddress} - Kullanıcı: {username} - Yasaklı Kelime: {badWord} - Tarih: {DateTime.Now}{Environment.NewLine}");
                }
            }

            public bool IsBanned(string ipAddress)
            {
                return bannedIPs.Contains(ipAddress);
            }

            private void LoadBannedIPs()
            {
                if (File.Exists(banFilePath))
                {
                    var lines = File.ReadAllLines(banFilePath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('-');
                        if (parts.Length > 0)
                        {
                            string ipAddress = parts[0].Trim();
                            bannedIPs.Add(ipAddress);
                        }
                    }
                }
            }
        }

        public class BadWordFilter
        {
            public List<string> badWords = new List<string> {  "elma", "Elma", "ELMA", "e lma", "e  lma", "e l m a", "e-lma", "e_lma", "e|lma", "@lma", "3lma", "e1ma", "e1m4", "e|ma", "e!ma", "e lmaaaa",
"e.lma", "el.ma", "elm.a", "elmaaaa", "e|1m@", "3|m@", "e+lma", "e~lma", "e@lma", "el.m.a", "el.maaa", "e*lm@",

"armut", "Armut", "ARMUT", "a rmut", "a-rmut", "@rmut", "4rmut", "ar_mu7", "a  r  m  u  t", "ar m-ut", "armu7", "ar.mut", "ar|mut",
"a~rmut", "a rm u t", "armutttt", "a|r|m|u|t", "ar,m,u,t", "arm.ut", "a_rmu7", "arm@ut", "a+rmut", "ar.mu.t", "armutttt", "a!rm!ut",

"çilek", "Çilek", "ÇİLEK", "ç i lek", "c ilek", "c!lek", "ç ilekkkkk", "c_ilek", "ç_i_le_k", "@ilek", "ç ilek",
"ç.i.lek", "ci1ek", "ç|lek", "ç*ilek", "ç!lek", "ç-i-lek", "ç1lek", "çi_lek", "ç.i.le.k", "ç_il_ek", "ç+ilek", "ç.i.lekkkk", "ç!lekkkk",

"kiraz", "Kiraz", "KİRAZ", "ki raz", "k!raz", "k1raz", "k_r@z", "k_r a z", "kir a zzzz", "k*ra*z", "ki!raz",
"kira.z", "ki|raz", "k1r4z", "kir@@z", "k_r-a-z", "k.ir.az", "k!r.az", "k1.ra.z", "kir~az", "k~i~r~a~z", "ki.ra.zzz", "kir+az",

"üzüm", "Üzüm", "ÜZÜM", "u züm", "u_züm", "uZ m", "u  Z  ü m", "u_z_um", "@züüm", "u$um", "u_zü_m", "u~zum",
"uz.um", "uz|um", "u~zu~m", "u zuuum", "uz|u|m", "uzzzum", "u+zum", "ü.z.üm", "uz_um", "ü$üm", "üzümüm", "u_zü_mm",

"portakal", "Portakal", "PORTAKAL", "P  ortakal", "P-o-r-t-a-k-a-l", "P@rtakal", "P0rt4k4l", "po rt akal", "portakal...", "por|takal",
"p+ortakal", "p_o_rt_a_k_a_l", "port@kal", "p0rtakal", "portakaaal", "po~rta~kal", "p*ortak@l", "p0r.ta.kal", "por!takal", "po_rtakal",

"mandalina", "Mandalina", "MANDALİNA", "m a n d a l i n a", "m a n d 4 l i n a", "m@nd@lina", "m4nd4lin4", "mand ali na", "manda|lina",
"m~anda~lina", "m!and!al!ina", "mandalinaa", "manda1ina", "mand4lin@", "man~da~lina", "mand~alina", "m+a+n+d+a+l+i+n+a", "mand@lin@", "m4nda|lina",

"karpuz", "Karpuz", "KARPUZ", "kar puz", "k a r p u z", "k a r p u z z z", "k!rp uz", "k r p u z", "k@rpu z", "k4r p uz", "k-r-pu-z",
"kar.puz", "k|arpuz", "kar+pu+z", "karpuuuz", "k@rpuz", "k@r.puz", "k+r.puz", "k-r.puz", "kar~puz", "karpuzz", "kar+pu+zz",

"kavun", "Kavun", "KAVUN", "k@v un", "K 4 v un", "k avvvvunnn", "k_av  un", "k-a-v-u-n", "k4v u n", "ka~vun",
"ka.v.un", "ka|vun", "k~avun", "kavunnn", "ka!v!un", "k+avun", "k~a~vun", "k~a~v~u~n", "k4vu~n", "k@vun", "ka~vun~", "ka~v.u~n",

"şeftali", "Şeftali", "ŞEFTALİ", "ş ef tali", "s e f t a l i", "ş ef+ali", "ş eft ali", "ş e f t a l i", "s!ef+ali", "ş~ef+tali",
"ş.e.f.t.a.l.i", "s+eftali", "s|eftali", "s3ftali", "s#eftali", "ş+e+f+t+a+l+i", "s~ef+tali", "şefta~li", "ş.ef.ta.li", "ş~e~ftali",

"ayva", "Ayva", "AYVA", "a y va", "4 y v a", "a y-v a", "a!yva", "ayvaa", "ay!va", "a_yv@",
"ayv+a", "a~yv~a", "ay.v.a", "ay~va", "ayvvva", "ay~v+a", "a.yv+va", "ay.vvv.a", "ay!v!a", "ayva~",

"ananas", "Ananas", "ANANAS", "a n a n as", "@nan@s", "a|n4n4s", "anan as", "anan @s", "anan.a.s", "ana~nas",
"an.an.as", "an+a+n+a+s", "ananasss", "anan@s", "anan~as", "an+a+n+a+s", "an!anas", "anan.a.s.s", "an~an~as", "anan+ass",

"vişne", "Vişne", "VİŞNE", "v i ş n e", "v i s n e", "vi$ne", "v issne", "vis!ne", "v+i+s+n+e", "v+isne",
"v~is~ne", "v.i.s.n.e", "visssne", "viss.ne", "v+i+şn+e", "v~i~ş~ne", "vi~sn~e", "vis.n.e",

"erik", "Erik", "ERİK", "€ r i k", "e r rrik", "er!ik", "e.r.i.k", "e!rik", "e|rik", "e+rik", "eriikkk",
"e~ri~k", "e!r.i.k", "er+ik", "erik~", "erikkk", "e+ri+k", "e+ri~k", "e.ri.k", "e.r.i.k.k", "er!i!k",

"greyfurt", "Greyfurt", "GREYFURT", "g r ey fu r t", "gr ey furt", "gr€y f urt", "g rey + furt", "g~reyfurt", "g~r~ey~furt",
"grey!furt", "g.re.y.fu.rt", "grey-furt", "greyfuurt", "g.r.e.y.f.u.r.t", "grey+furt", "g~rey+furt", "gr3y~furt", "g~r~fu~rt"
   };

            public bool ContainsBadWord(string message)
            {
                return badWords.Any(word => message.ToLower().Contains(word.ToLower()));
            }
        }

        public byte[] ObjectToByteArray(Object obj)
        {
            using (var ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public Object ByteArrayToObject(byte[] arrBytes)
        {
            using (var memStream = new MemoryStream(arrBytes))
            {
                return new BinaryFormatter().Deserialize(memStream);
            }
        }
    }
}
