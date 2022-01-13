using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using InTheHand.Net;
using System.IO;

namespace BLEScanner
{
    public partial class homepage : Form
    {
        public homepage()
        {
            InitializeComponent();
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;
        }

        SqlConnection conn = new SqlConnection("Data Source=JARVIS;Initial Catalog=BLEScannerDB;Integrated Security=True;MultipleActiveResultSets=true");
        int sayac = 0;
        int presentScanID = 0;
        string districtOfScan = "";
        string districtName = "";
        string currentDateTime = "";
        string previousDateTime = "";
        string insertOrUpdateState = "";
        int startState = 0;

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox2.Text = "0";
            timer1.Interval = 1000;
            BindGrid();

            SqlDataReader dr;
            DataSet ds = new DataSet();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            cmd.CommandText = "SELECT Name FROM Districts";
            conn.Open();
            dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                cmxDistricts.Items.Add(dr["Name"]); ;
            }
            conn.Close();
            dr.Close();
        }

        async void ScanForMSSQL()
        {
            SqlDataReader dr;
            DataSet ds = new DataSet();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            currentDateTime = DateTime.Now.ToString();

            cmd.CommandText = "SELECT * FROM Devices";
            conn.Open();
            dr = cmd.ExecuteReader();
            if (dr.HasRows) startState = 1;
            else startState = 0;
            conn.Close();
            dr.Close();
            cmd.Parameters.Clear();

            if (startState == 0)
            {
                cmd.CommandText = "INSERT INTO Devices (ScanID, Name, Address, District, CheckIn, CheckOut) " +
                         "values (@scanID, @name, @address, @district, @checkIn, @checkOut)";
                cmd.Parameters.AddWithValue("@scanID", 0);
                cmd.Parameters.AddWithValue("@name", "Name");
                cmd.Parameters.AddWithValue("@address", "Address");
                cmd.Parameters.AddWithValue("@district", districtOfScan);
                cmd.Parameters.AddWithValue("@checkIn", currentDateTime);
                cmd.Parameters.AddWithValue("@checkOut", currentDateTime);

                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
                cmd.Parameters.Clear();
            }

            cmd.CommandText = "SELECT MAX(ScanID) AS ScanIDcounter, MAX(CheckOut) AS PreviousCheckOut FROM Devices";
            conn.Open();
            dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                presentScanID = Convert.ToInt32((dr["ScanIDcounter"]).ToString());
                previousDateTime = (dr["PreviousCheckOut"]).ToString();
            }
            conn.Close();
            dr.Close();

            presentScanID += 1;

            BluetoothRadio.PrimaryRadio.Mode = RadioMode.Connectable;
            BluetoothClient client = new BluetoothClient();
            BluetoothDeviceInfo[] devices = client.DiscoverDevices();
            BluetoothClient bluetoothClient = new BluetoothClient();

            foreach (BluetoothDeviceInfo device in devices)
            {
                cmd.CommandText = "SELECT * FROM Devices WHERE Name = @name AND Address = @address AND District = @district AND Checkout = @previousTime";
                cmd.Parameters.AddWithValue("@name", device.DeviceName.ToString());
                cmd.Parameters.AddWithValue("@address", device.DeviceAddress.ToString());
                cmd.Parameters.AddWithValue("@district", districtOfScan);
                cmd.Parameters.AddWithValue("@previousTime", previousDateTime);
                conn.Open();
                dr = cmd.ExecuteReader();
                if (dr.HasRows) insertOrUpdateState = "update";
                else insertOrUpdateState = "insert";
                txtInsertsUpdates.Invoke((MethodInvoker)(() => txtInsertsUpdates.Text = Environment.NewLine + device.DeviceName.ToString() + " " +
                    device.DeviceAddress.ToString() + " " + insertOrUpdateState + Environment.NewLine + txtInsertsUpdates.Text));
                conn.Close();
                dr.Close();
                cmd.Parameters.Clear();

                if (insertOrUpdateState == "insert")
                {
                    cmd.CommandText = "INSERT INTO Devices (ScanID, Name, Address, District, CheckIn, CheckOut) " +
                         "values (@scanID, @name, @address, @district, @checkIn, @checkOut)";
                    cmd.Parameters.AddWithValue("@scanID", presentScanID);
                    cmd.Parameters.AddWithValue("@name", device.DeviceName.ToString());
                    cmd.Parameters.AddWithValue("@address", device.DeviceAddress.ToString());
                    cmd.Parameters.AddWithValue("@district", districtOfScan);
                    cmd.Parameters.AddWithValue("@checkIn", currentDateTime);
                    cmd.Parameters.AddWithValue("@checkOut", currentDateTime);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                    cmd.Parameters.Clear();
                }
                else if (insertOrUpdateState == "update")
                {
                    //update tEmployees set ePassword=@password where eUserName=@username
                    cmd.CommandText = "UPDATE Devices SET CheckOut = @checkOut WHERE Name = @name AND Address = @address" +
                        " AND District = @district AND CheckOut = @previousTime";
                    cmd.Parameters.AddWithValue("@name", device.DeviceName.ToString());
                    cmd.Parameters.AddWithValue("@address", device.DeviceAddress.ToString());
                    cmd.Parameters.AddWithValue("@district", districtOfScan);
                    cmd.Parameters.AddWithValue("@previousTime", previousDateTime);
                    cmd.Parameters.AddWithValue("@checkOut", currentDateTime);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                    cmd.Parameters.Clear();
                }
            }
        }

        private void BindGrid()
        {
            SqlDataReader dr;
            DataSet ds = new DataSet();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            cmd.CommandText = "SELECT ScanID, ID, Name, Address, District, CheckIn, CheckOut FROM Devices";
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            conn.Open();
            da.Fill(ds, "Devices");
            dgDevices.DataSource = ds.Tables["Devices"];
            conn.Close();
        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            BindGrid();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            //if (backgroundWorker1.IsBusy != true) {
            //    backgroundWorker1.RunWorkerAsync();
            //}
            SqlDataReader dr;
            DataSet ds = new DataSet();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            if (cmxDistricts.SelectedItem != null)
            {
                cmd.CommandText = "SELECT Name FROM Districts WHERE Name = @name";
                cmd.Parameters.AddWithValue("@name", cmxDistricts.SelectedItem.ToString());
                conn.Open();
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    districtOfScan = (dr["Name"]).ToString(); /*Convert.ToInt32*/
                }
                conn.Close();
                dr.Close();
                MessageBox.Show(cmxDistricts.SelectedItem.ToString() + " için tarama başlatıldı!");
                timer1.Start();
            }
            else
            {
                MessageBox.Show("Please select a district to start scanning!");
            }

        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            //if (backgroundWorker1.WorkerSupportsCancellation == true)
            //{
            //    // Cancel the asynchronous operation.
            //    backgroundWorker1.CancelAsync();
            //}
            timer1.Stop();
            backgroundWorker1.CancelAsync();
            sayac = 0;
            textBox2.Text = sayac.ToString();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            sayac++;
            textBox2.Text = sayac.ToString();
            if (backgroundWorker1.IsBusy != true)
            {
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            ScanForMSSQL();
        }

        private void dgDevices_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void btnAddDistrict_Click(object sender, EventArgs e)
        {
            SqlDataReader dr;
            DataSet ds = new DataSet();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            cmd.CommandText = "SELECT districtName FROM tDistricts WHERE districtName = @name";
            cmd.Parameters.AddWithValue("@name", txtDistrict.Text);
            conn.Open();
            dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                districtName = (dr["districtName"]).ToString();
            }
            conn.Close();
            dr.Close();
            cmd.Parameters.Clear();

            if (districtName == "")
            {
                cmd.CommandText = "INSERT INTO tDistricts (districtName) values (@name)";
                cmd.Parameters.AddWithValue("@name", txtDistrict.Text);
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
                cmd.Parameters.Clear();
                MessageBox.Show("District created!");
            }
            else
            {
                MessageBox.Show("Please enter a district name that has not been created before!");
            }

        }
    }
}

