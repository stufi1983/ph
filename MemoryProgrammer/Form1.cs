using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

using System.ComponentModel.Design;

namespace MemoryProgrammer
{
    public partial class Form1 : Form
    {
        String[] Ports = { "Paralel", "Paralel-SPI", "Serial-SPI", "Serial"};
        String[] ParDev = { "Generic" };
        String[] SPIDev = { "93C66" };
        String[] SerDev = { "Generic" };

        double kecepatan = 0.5;
        string portName = "0";
        int portAddress = 888;
        string selectedIC = "";
        string selectedPorts = "";

        String hex = "";
        public int progress = 0;

        Thread ReadWriteThread;

        ParallelPort parPort = new ParallelPort();
        SerialPort ReadDataSerial;

        private System.ComponentModel.Design.ByteViewer byteviewer;

        public Form1()
        {
            InitializeComponent();

            // Initialize the ByteViewer.
            byteviewer = new ByteViewer();

            byteviewer.Location = textBoxString.Location;// new Point(8, 46);
            byteviewer.Size = textBoxString.Size;// new Size(600, 338);
            byteviewer.Anchor = AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Top;
            byteviewer.SetBytes(new byte[] { });
            this.Controls.Add(byteviewer);
            textBoxString.Visible = false;
        }
        private void btnBukaFile_Click(object sender, EventArgs e)
        {
            BukaFile();
        }

        private void ConvertHextoBytes()
        {
            hex = "";
            for (int i = 0; i < textBoxString.Lines.Length; i++)
            {
                hex += textBoxString.Lines[i];
            }

        }

        private void BukaFile()
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "Hex Files|*.HEX";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    readDataFromFile(openFileDialog1.FileName);
                    byteviewer.SetFile(openFileDialog1.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Gagal membuka file: " + ex.Message);
                }
            }
        }
        void readDataFromFile(String filename)
        {
            String line = "";
            try
            {
                using (StreamReader sr = new StreamReader(filename))
                {
                    textBoxString.Text = "";
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.StartsWith("\0"))
                            line = "";
                        else
                            textBoxString.Text += line + Environment.NewLine;
                    }
                    sr.Close();
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("readFromFileString " + filename, err.Message);
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (string Port in Ports)
                cmbPort.Items.Add(Port);
            cmbPort.SelectedItem = Ports[3];
            cmbIC.SelectedItem = ParDev[0];
        }

        private void btnWrite_Click(object sender, EventArgs e)
        {
            ConvertHextoBytes();
            //if (textBoxString.Text == "")
            if(byteviewer.GetBytes().Length<1)
            {
                MessageBox.Show("File belum dibuka");
                return;
            }

            if (cmbPort.SelectedItem.ToString() == null)
            {
                MessageBox.Show("Port belum dipilih");
                return;
            }
            selectedPorts = cmbPort.SelectedItem.ToString();

            if (cmbIC.SelectedItem.ToString() == null)
            {
                MessageBox.Show("IC belum dipilih");
                return;
            }

            selectedIC = cmbIC.SelectedItem.ToString();
            portName = txtAlamat.Text;
            int.TryParse(txtAlamat.Text, out portAddress);
            if (portAddress <= 0) portAddress = 888;
            Double.TryParse(txtSpeed.Text, out kecepatan);
            if (kecepatan <= 0) kecepatan = 1;
            int interval = (int)(1000 / kecepatan);
            if (interval <= 0) interval = 1;
            timer1.Interval = interval;


            if (cmbPort.SelectedItem.ToString() == Ports[0]) //Par
            {
                ReadWriteThread = new Thread(KirimParalel);
            }
            else if (cmbPort.SelectedItem.ToString() == Ports[1]  //Par-SPI
                || cmbPort.SelectedItem.ToString() == Ports[2]) //Ser-SPI
            {
                ReadWriteThread = new Thread(KirimSPI);
            }
            else if (cmbPort.SelectedItem.ToString() == Ports[3]) //Serial
            {
                ReadWriteThread = new Thread(KirimSerial);
            }
            button2.Enabled = false;
            progress = 0; timer1.Enabled = true;
            ReadWriteThread.Start();
        }
        void KirimSerial()
        {
            SerialPort koneksiSerial = new SerialPort(portName, (int)kecepatan);
            koneksiSerial.Handshake = Handshake.None;
            try
            {
                koneksiSerial.Open();
                byte[] bytes = byteviewer.GetBytes();
                hex = BitConverter.ToString(bytes).Replace("-", string.Empty);
                int length = hex.Length;

                int i = 0;
                while (i < length)
                {
                    //TODO:w-->w1,w2,w3
                    if (length >= i + 512)
                        koneksiSerial.WriteLine("w" + hex.Substring(i, i + 512));
                    else
                        koneksiSerial.WriteLine("w" + hex.Substring(i, length-i));

                    progress = (int)(i * 100 / length);
                    i += 512;
                    Thread.Sleep(5000);
                    
                }
                koneksiSerial.Close();
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message, "Error");
            }
            progress = 100;
            koneksiSerial.Close();
        }
        void KirimSPI()
        {

            if (selectedIC == "93C66")
            {
                IC.EEPROM_93C66 ic;
                if (selectedPorts == Ports[1])
                    ic = new MemoryProgrammer.IC.EEPROM_93C66(portAddress, 500000, this);
                else if (selectedPorts == Ports[2])
                    ic = new MemoryProgrammer.IC.EEPROM_93C66(portName, 50, this);
                else
                    return;

                //string[] split = new string[hex.Length / 2 + (hex.Length % 2 == 0 ? 0 : 1)];
                //for (int i = 0; i < split.Length; i++)
                //{
                //    split[i] = hex.Substring(i * 2, i * 2 + 2 > hex.Length ? 1 : 2);
                //}

                try { ic.StartTransfer(); }
                catch (Exception err) { MessageBox.Show(err.Message); return; }

                timer1.Enabled = true;
                ic.EEPROM_93C66_spi_ewen();
                Thread.Sleep(1);

                //write all
                //ic.EEPROM_93C66_writeAll(ref split);
                byte[] bytes = byteviewer.GetBytes();
                for(int x = 0;x<bytes.Length;x+=2)
                {
                    int data = 0;
                    data = data | bytes[x];
                    data = data << 8;
                    data = data | bytes[x+1];
                    ic.EEPROM_93C66_spi_send_data(x,data);
                    progress = x * 100 / bytes.Length;
                }
                ic.StopTransfer();
                progress = 100;
            }
        }

        void KirimParalel()
        {

            int portAddress = 888;
            int.TryParse(txtAlamat.Text, out portAddress);
            if (portAddress < 1) portAddress = 888;
            parPort.Address = portAddress;

            String initRes = "";
            initRes = parPort.init();
            if (initRes != "")
            {
                MessageBox.Show(initRes);
                return;
            }

            //string[] split = new string[hex.Length / 2 + (hex.Length % 2 == 0 ? 0 : 1)];
            //for (int i = 0; i < split.Length; i++)
            //{
            //    split[i] = hex.Substring(i * 2, i * 2 + 2 > hex.Length ? 1 : 2);
            //}

            //for (int i = 0; i < split.Length; i++)
            //{
            //    int bytes = Convert.ToInt32(split[i], 16);
            //    parPort.SendByte(bytes);
            //    progress = (int)(i * 100 / split.Length);
            //    System.Threading.Thread.Sleep(timer1.Interval);
            //}
            byte[] b = byteviewer.GetBytes();
            for (int i = 0; i < b.Length; i++) {
                parPort.SendByte((int)b[i]);
                progress = (int)(i * 100 / b.Length);
                System.Threading.Thread.Sleep(timer1.Interval);
            }

            progress = 100;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            progressBar1.Value = progress;
            if (progressBar1.Value >= 100)
            {
                button2.Enabled = true;
                timer1.Enabled = false;
            }
        }

        private delegate void SetTextDeleg(string data);
        void tampilkanData(string data)
        {
            textBoxString.Text = data.Trim();

        }

        private void cmbPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbIC.Items.Clear();

            if (cmbPort.SelectedItem.ToString() == Ports[0])
            {
                txtSpeed.Text = "8";
                txtAlamat.Text = "888";
                label5.Text = "Byte/s";
                foreach (String dev in ParDev)
                    cmbIC.Items.Add(dev);
                cmbIC.SelectedItem = ParDev[0];
            }
            if (cmbPort.SelectedItem.ToString() == Ports[1]
                || cmbPort.SelectedItem.ToString() == Ports[2])
            {
                txtSpeed.Text = "1000";
                if (cmbPort.SelectedItem.ToString() == Ports[1])
                    txtAlamat.Text = "888";
                if (cmbPort.SelectedItem.ToString() == Ports[1])
                    txtAlamat.Text = "COM1";
                label5.Text = "Hertz";
                foreach (String dev in SPIDev)
                    cmbIC.Items.Add(dev);
                cmbIC.SelectedItem = SPIDev[0];
            }
            if (cmbPort.SelectedItem.ToString() == Ports[3])
            {
                txtSpeed.Text = "57600";
                txtAlamat.Text = "COM1";
                label5.Text = "bps";
                foreach (String dev in SerDev)
                    cmbIC.Items.Add(dev);
                cmbIC.SelectedItem = ParDev[0];
            }
        }

        private void btnRead_Click(object sender, EventArgs e)
        {
            if (cmbPort.SelectedItem.ToString() == null)
            {
                MessageBox.Show("Port belum dipilih");
                return;
            }
            selectedPorts = cmbPort.SelectedItem.ToString();

            if (cmbIC.SelectedItem.ToString() == null)
            {
                MessageBox.Show("IC belum dipilih");
                return;
            }

            selectedIC = cmbIC.SelectedItem.ToString();
            portName = txtAlamat.Text;
            int.TryParse(txtAlamat.Text, out portAddress);
            Double.TryParse(txtSpeed.Text, out kecepatan);
            int interval = (int)(1000 / kecepatan);
            if (interval <= 0) interval = 1;
            timer1.Interval = interval;


            if (cmbPort.SelectedItem.ToString() == Ports[3]) //Serial
            {
                ReadWriteThread = new Thread(ReadDataSerialThread);
            }
            if (cmbPort.SelectedItem.ToString() == Ports[2] ||//Serial-SPI
                cmbPort.SelectedItem.ToString() == Ports[1]) //Parallel-SPI
            {
                ReadWriteThread = new Thread(ReadDataSPIThread);
            }
            if(ReadWriteThread!=null) ReadWriteThread.Start();
            timer1.Enabled = true;
        }
        void ReadDataSPIThread() //ReadSerial/ParSPI
        {
            if (selectedIC == "93C66")
            {
                IC.EEPROM_93C66 ic;
                if (selectedPorts == Ports[1])
                    ic = new MemoryProgrammer.IC.EEPROM_93C66(portAddress, 500000, this);
                else if (selectedPorts == Ports[2])
                    ic = new MemoryProgrammer.IC.EEPROM_93C66(portName, 50, this);
                else
                    return;
                timer1.Enabled = true;
                try { ic.StartTransfer(); }
                catch (Exception err) { MessageBox.Show(err.Message); return; }

                hex = ""; 
                for (int x = 0; x < 256; x++)
                {
                    UInt32 val = ic.EEPROM_93C66_spi_read_data(x);
                    hex += val.ToString("X4");
                    progress = x / 256 * 100;
                }
                ic.StopTransfer();
                this.BeginInvoke(new SetTextDeleg(tampilkanData),
                new object[] { hex });
                byteviewer.SetBytes(StringToByteArray(hex));
                progress = 100;
            }
        }


        void ReadDataSerialThread() //ReadSerial, generic
        {
            int baud = 57600;
            int.TryParse(txtSpeed.Text, out baud);
            portName = txtAlamat.Text;

            ReadDataSerial = new SerialPort(portName, baud);
            ReadDataSerial.ReadBufferSize = 8192;
            ReadDataSerial.WriteBufferSize = 100;
            try
            {

                if (!ReadDataSerial.IsOpen)
                {
                    ReadDataSerial.PortName = portName;
                    ReadDataSerial.Open();
                    //ReadDataSerial.DataReceived += new SerialDataReceivedEventHandler(saatDataDiterima);
                }
            }
            catch (Exception ee)
            {
                // ReadDataSerial.Dispose();
                MessageBox.Show(ee.Message, "Error");
                try
                {
                    if (ReadDataSerial.IsOpen) ReadDataSerial.Close();
                }
                catch { }

            }

            ReadDataSerial.WriteLine("r");

            //for (int x = 0; x <= 100; x++)
            //{
            //    progress = x;
            //    Thread.Sleep(25);
            //}
            
            int intReturnASCII = 0;
            int count = 0;
            while (count < 512) {
                try { count = ReadDataSerial.BytesToRead; }
                catch { }
                progress = (int)(count * 100 / 512);
            }
            string returnMessage = "";
            while (count > 0)
            {
                try { intReturnASCII = ReadDataSerial.ReadByte(); }
                catch { }
                returnMessage = returnMessage + Convert.ToChar(intReturnASCII);
                count--;
            }
            this.BeginInvoke(new SetTextDeleg(tampilkanData),
                            new object[] { returnMessage });
            
            if (ReadDataSerial.IsOpen)
                ReadDataSerial.Close();
            byteviewer.SetBytes(StringToByteArray(textBoxString.Text));

            progress = 100;
        }
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        private void button4_Click(object sender, EventArgs e)
        {
            byteviewer.SetBytes(new byte[] { });
        }

    }
}
