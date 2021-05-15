/*
 * Autore: Pulga Luca
 * Classe: 4^L
 * Data: 2021-05-04
 * Il processo che stiamo creando deve essere in grado di ascoltare cio che gli arriva della rete.
 * Se il processo si occupa di ascolater la rete, deve fare solo questo, e successivamente dovrà essere in grado di inviare.
 * Dovremo creare un thread per ascoltare il canale e allo stesso tempo funzioni per l'invio verso il destinatario.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
// Aggiungi librerie.
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text.RegularExpressions;

namespace ComunicazioneSocket2
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Source socket.
            // IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 55000); // uso loopback o anche indirizzo di 192.168.1.73
            IPEndPoint localEndPointAutomated = new IPEndPoint(IPAddress.Parse(GetLocalIPAddress()), 56000); // uso loopback o anche indirizzo di 192.168.1.73

            txtIpAdd.Text = localEndPointAutomated.Address.ToString();
            txtDestPort.Text = "55000";

            Thread t1 = new Thread(new ParameterizedThreadStart(SocketReceive)); // Parametizzazione di un thread.
            t1.Start(localEndPointAutomated);
        }

        public async void SocketReceive(object sourceEndPoint) // Programmazione asicrona, ascolta e continua ad utilizzare l'interfaccai perchè le 2 cose, ascolto e invio vengono supportate.
        {
            IPEndPoint sourceEP = (IPEndPoint)sourceEndPoint;

            // socket da cui riceveremo.
            // IPv4 e altre info    Tipo di socket    Tipo di protocollo.
            Socket t = new Socket(sourceEP.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            t.Bind(sourceEP); // Associa un socket ad un endPoint.

            Byte[] byteRicevuti = new byte[256]; // Max ricevo 256 byte

            string message = "";
            int bytes = 0; // contatore byte ricevuti.

            // Thread continua ad ascoltare e ricevere i byte.
            // Task parte di thread.
            await Task.Run(() =>
            {
                while (true)
                {
                    // Ci avvisa quando sul socket sono arrivati dei dati.
                    if (t.Available > 0)
                    {
                        // Ricezione
                        bytes = t.Receive(byteRicevuti, byteRicevuti.Length, 0);
                        // Prendo tutti i caratteri che ho messo dentro al vettore di byte, per ogni carattere e  li concateno all0interno del messaggio.
                        message += Encoding.ASCII.GetString(byteRicevuti, 0, bytes);

                        // Gestione elementi grafici difficoltosa e non si può fare così
                        // lblRicezione.Content = message;

                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            lblRicezione.Text += $"\n{DateTime.Now} \n=> Host: " + message;
                        }));
                    }
                }
            });
        }

        private void btnInvia_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (txtDestPort.Text.Length == 0)
                    throw new Exception("Immettere una porta.");
                if (txtIpAdd.Text.Length == 0)
                    throw new Exception("Immettere un ip.");
                if (txtMsg.Text.Length == 0)
                    throw new Exception("Immettere un messaggio.");

                if (!IsIPv4(txtIpAdd.Text))
                    throw new Exception("Immettere un indirizzo ip valido.");

                IPAddress ipDest = IPAddress.Parse(txtIpAdd.Text); // Recupero informazioni ip del destinatario.
                int portDest = int.Parse(txtDestPort.Text); // Porta di destinazione.

                IPEndPoint remoteEndPoint = new IPEndPoint(ipDest, portDest);

                // Socket abbinato al socket primario.
                Socket s = new Socket(ipDest.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                // Leggo direttamente dall'interfaccia e scompatto direttamente in byte.
                Byte[] byteInviati = Encoding.ASCII.GetBytes(txtMsg.Text);

                s.SendTo(byteInviati, remoteEndPoint); // byte e a chi vogliamo mandarli.

                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    lblRicezione.Text += $"\n{DateTime.Now} \n=> You: " + txtMsg.Text;
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Errore nell'invio del messaggio.\n" + ex.Message, "Errore nell'invio dei dati", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

        }

        /// <summary>
        /// get automated local ip.
        /// </summary>
        /// <returns></returns>
        public static string GetLocalIPAddress()
        {
            try
            {
                string hostName = Dns.GetHostName(); // Retrive the Name of HOST  
                // Get the IP  
                string myIP = Dns.GetHostByName(hostName).AddressList[0].ToString(); // Get local ip address

                Uri uri = new Uri("http://" + myIP);

                return uri.Host.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("No network adapters with an IPv4 address in the system!");
            }
        }

        /// <summary>
        /// control ipv4 address.
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public static bool IsIPv4(string ipAddress)
        {
            return Regex.IsMatch(ipAddress, @"^\d{1,3}(\.\d{1,3}){3}$") && ipAddress.Split('.').SingleOrDefault(s => int.Parse(s) > 255) == null; // Regex per controllare ip.
        }
    }
}
