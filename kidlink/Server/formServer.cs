using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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

        // IP Ban Manager
        IPBanManager ipBanManager = new IPBanManager();

        // BadWord Filter
        BadWordFilter badWordFilter = new BadWordFilter();

        // Küfürlü kelime sayacını tutan sözlük
        Dictionary<string, int> userBadWordCount = new Dictionary<string, int>();

        public Server()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            cancellation = new CancellationTokenSource(); // Resets the token when the server restarts
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
            updateUI("Sunucu global bağlantılar için dinlemeye başladı (Port: 5000).");

            try
            {
                int counter = 0;
                while (true)
                {
                    counter++;
                    client = await Task.Run(() => listener.AcceptTcpClientAsync(), cancellation.Token);
                    string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();  // IP adresini al

                    // IP adresini kontrol et
                    if (ipBanManager.IsBanned(clientIP))
                    {
                        client.Close();
                        continue; // Yasaklı IP varsa, bağlantıyı kes
                    }

                    // get username
                    byte[] name = new byte[50];
                    NetworkStream stre = client.GetStream();
                    stre.Read(name, 0, name.Length);
                    String username = Encoding.ASCII.GetString(name);
                    username = username.Substring(0, username.IndexOf("$"));

                    // Kullanıcıyı ekle
                    lock (clientList)
                    {
                        clientList.Add(username, client);
                    }

                    this.Invoke((MethodInvoker)delegate
                    {
                        listBox1.Items.Add(username);
                    });

                    updateUI("Kullanıcı bağlandı " + username + " - " + client.Client.RemoteEndPoint);
                    announce(username + " Katıldı ", username, false);

                    // Küfür sayacını sıfırla
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

        // IPBanManager sınıfı
        public class IPBanManager
        {
            private HashSet<string> bannedIPs = new HashSet<string>();
            private string banFilePath = "banned_ips.txt";

            public IPBanManager()
            {
                LoadBannedIPs();
            }

            // IP'yi yasakla
            public void BanIP(string ipAddress)
            {
                bannedIPs.Add(ipAddress);
                SaveBannedIPs();
            }

            // Yasaklı IP'yi kontrol et
            public bool IsBanned(string ipAddress)
            {
                return bannedIPs.Contains(ipAddress);
            }

            // Yasaklı IP'leri dosyaya kaydet
            private void SaveBannedIPs()
            {
                File.WriteAllLines(banFilePath, bannedIPs);
            }

            // Dosyadan yasaklı IP'leri yükle
            private void LoadBannedIPs()
            {
                if (File.Exists(banFilePath))
                {
                    var bannedIPsFromFile = File.ReadAllLines(banFilePath);
                    bannedIPs = new HashSet<string>(bannedIPsFromFile);
                }
            }
        }

        // Badword filtreleme sınıfı
        public class BadWordFilter
        {
            private List<string> badWords = new List<string> { "küfür1", "küfür2", "küfür3" }; // Küfürlü kelimeler

            public bool ContainsBadWord(string message)
            {
                foreach (var word in badWords)
                {
                    if (message.ToLower().Contains(word.ToLower()))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public void announce(string msg, string uName, bool flag)
        {
            try
            {
                if (badWordFilter.ContainsBadWord(msg))
                {
                    msg = "Küfürlü kelime içeriyor ve engellendi.";

                    // Kullanıcının küfürlü kelime sayısını artır
                    if (userBadWordCount.ContainsKey(uName))
                    {
                        userBadWordCount[uName]++;
                    }
                    else
                    {
                        userBadWordCount[uName] = 1;
                    }

                    // Eğer 3 kere küfürlü kelime kullanıldıysa, IP banla ve bağlantıyı kes
                    if (userBadWordCount[uName] >= 3)
                    {
                        TcpClient bannedClientForIP = (TcpClient)clientList.FirstOrDefault(x => x.Key == uName).Value;
                        if (bannedClientForIP != null)
                        {
                            string clientIP = ((IPEndPoint)bannedClientForIP.Client.RemoteEndPoint).Address.ToString();
                            ipBanManager.BanIP(clientIP); // Yasakla
                            bannedClientForIP.Close(); // Bağlantıyı kes
                        }
                    }
                }

                foreach (var Item in clientList)
                {
                    TcpClient broadcastSocket = (TcpClient)Item.Value;
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
            catch (Exception er)
            {
                // Hata durumunda yapılacak işlemler
            }
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

                    // Mesajın geçerli formatta olup olmadığını kontrol et
                    if (parts.Count < 2)
                    {
                        updateUI("Geçersiz mesaj formatı: " + username);
                        break;
                    }

                    switch (parts[0])
                    {
                        case "gChat":
                            if (badWordFilter.ContainsBadWord(parts[1]))
                            {
                                parts[1] = "Küfürlü kelime içeriyor ve engellendi.";

                                // Küfürlü kelime tespit edilirse kullanıcının IP'sini yasakla
                                TcpClient bannedClient = (TcpClient)clientList.FirstOrDefault(x => x.Key == username).Value;
                                if (bannedClient != null)
                                {
                                    string clientIP = ((IPEndPoint)bannedClient.Client.RemoteEndPoint).Address.ToString();
                                    ipBanManager.BanIP(clientIP); // Yasakla
                                    bannedClient.Close(); // Bağlantıyı kes
                                }
                            }

                            this.Invoke((MethodInvoker)delegate
                            {
                                textBox1.Text += username + ": " + parts[1] + Environment.NewLine;
                            });
                            announce(parts[1], username, true);
                            break;
                    }
                }
                catch (Exception r)
                {
                    updateUI("Kullanıcının Bağlantısı Kesildi: " + username);
                    announce("Kullanıcının Bağlantısı Kesildi: " + username + "$", username, false);
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
            try
            {
                listener.Stop();
                updateUI("Server Durduruldu");
                foreach (var Item in clientList)
                {
                    TcpClient broadcastSocket;
                    broadcastSocket = (TcpClient)Item.Value;
                    broadcastSocket.Close();
                }
            }
            catch (SocketException er)
            {
                // Hata durumunda yapılacak işlemler
            }
        }

        private void Private_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                String clientName = listBox1.GetItemText(listBox1.SelectedItem);

                chat.Clear();
                chat.Add("gChat");
                chat.Add("Admin : " + inputPrivate.Text);

                byte[] byData = ObjectToByteArray(chat);
                TcpClient workerSocket = null;
                workerSocket = (TcpClient)clientList.FirstOrDefault(x => x.Key == clientName).Value; //find the client by username in dictionary

                NetworkStream stm = workerSocket.GetStream();
                stm.Write(byData, 0, byData.Length);
                stm.Flush();
                chat.Clear();

            }
        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                TcpClient workerSocket = null;

                String clientName = listBox1.GetItemText(listBox1.SelectedItem);
                workerSocket = (TcpClient)clientList.FirstOrDefault(x => x.Key == clientName).Value; //find the client by username in dictionary
                workerSocket.Close();

            }
            catch (SocketException se)
            {
                // Hata durumunda yapılacak işlemler
            }
        }

        public void sendUsersList()
        {
            try
            {
                byte[] userList = new byte[1024];
                string[] clist = listBox1.Items.OfType<string>().ToArray();
                List<string> users = new List<string>();

                users.Add("userList");
                foreach (String name in clist)
                {
                    users.Add(name);
                }
                userList = ObjectToByteArray(users);

                foreach (var Item in clientList)
                {
                    TcpClient broadcastSocket;
                    broadcastSocket = (TcpClient)Item.Value;
                    NetworkStream broadcastStream = broadcastSocket.GetStream();
                    broadcastStream.Write(userList, 0, userList.Length);
                    broadcastStream.Flush();
                    users.Clear();
                }
            }
            catch (SocketException se)
            {
                // Hata durumunda yapılacak işlemler
            }
        }

        private void privateChat(List<string> text)
        {
            try
            {
                byte[] byData = ObjectToByteArray(text);

                TcpClient workerSocket = null;
                workerSocket = (TcpClient)clientList.FirstOrDefault(x => x.Key == text[1]).Value; //find the client by username in dictionary

                NetworkStream stm = workerSocket.GetStream();
                stm.Write(byData, 0, byData.Length);
                stm.Flush();

            }
            catch (SocketException se)
            {
                // Hata durumunda yapılacak işlemler
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            textBox1.SelectionStart = textBox1.TextLength;
            textBox1.ScrollToCaret();
        }

        // Menüde yasaklama işlemi
        private void banUserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string clientName = listBox1.GetItemText(listBox1.SelectedItem);
            TcpClient clientSocket = (TcpClient)clientList.FirstOrDefault(x => x.Key == clientName).Value;
            string clientIP = ((IPEndPoint)clientSocket.Client.RemoteEndPoint).Address.ToString();
            ipBanManager.BanIP(clientIP);
            MessageBox.Show("IP adresi yasaklandı: " + clientIP);
        }

        // ObjectToByteArray Metodu
        public byte[] ObjectToByteArray(Object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        // ByteArrayToObject Metodu
        public Object ByteArrayToObject(byte[] arrBytes)
        {
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(arrBytes, 0, arrBytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = binForm.Deserialize(memStream);
                return obj;
            }
        }
    }
}
