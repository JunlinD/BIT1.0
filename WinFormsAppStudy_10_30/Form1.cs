using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Management;
using System.IO;
namespace WinFormsAppStudy_10_30
{
   
    public partial class Form1 : Form
    {
        public const int SECTOR_SIZE = 512;
        private byte[] ByteArray=new byte[SECTOR_SIZE];
        private byte[] Byte0Array = new byte[SECTOR_SIZE];
        private byte[] ShowArray = new byte[SECTOR_SIZE];
        private DriverLoader driverLoader;
        public Form1()
        {
            InitializeComponent();
            initByteArray();
            driverLoader = new DriverLoader("E:");
            textBox1.Text = ""+driverLoader.SectorLength;

        }
        /*private void Show_SectorSize(object sender, EventArgs e)
        {
            
        }*/
        private void initByteArray()
        {
            for(int i=0;i<SECTOR_SIZE;i++)
            {
                ByteArray[i] = 255;
                ShowArray[i] = 0;
                Byte0Array[i] = 0;
            }
        }
        private void Button1_Click(object sender, EventArgs e)
        {
            ReadFirstSector(driverLoader);
        }
        private void Button2_Click(object sender, EventArgs e)
        {
            WriteFirstSector(driverLoader);
        }
        private void Button3_Click(object sender, EventArgs e)
        {
            WriteAllSector(driverLoader);
        }
        private void ReadFirstSector(DriverLoader driver)
        {
            if (driver == null||ByteArray[0]!=255)
                return;
            ShowArray = driver.ReadSector(0);
            outArray(ShowArray);
        }
        private void WriteFirstSector(DriverLoader driver)
        {
            if (driver == null || ByteArray[0] != 255)
                return;
            driver.WritSector(ByteArray, 0);
            ActivityLogTextBox.AppendText("写成功\r\n");
            ReadFirstSector(driver);
            if(VerifyWrite(ByteArray, ShowArray))
            {
                ActivityLogTextBox.AppendText("验证成功\r\n");
            }
        }
        private void WriteAllSector(DriverLoader driver)
        {
            if (driver == null || ByteArray[0] != 255)
                return;
            for(var i=0;i<1000;i++)
            {
                driver.WritSector(ByteArray, i);
            }
            ActivityLogTextBox.AppendText("写1000个成功\r\n");
        }
        private bool VerifyWrite(byte[] verifyarray,byte[] readarray)
        {
            var len1 = verifyarray.Length;
            var len2 = readarray.Length;
            if (len1 != len2)
            { 
                ActivityLogTextBox.AppendText("数组长度不匹配\r\n");
                return false;
            }
            for(var i=0;i<len1;i++)
            {
                if (verifyarray[i] != readarray[i])
                {
                    ActivityLogTextBox.AppendText("写验证出错，出错位置是" + i + "\r\n");
                    return false;
                }
            }
            return true;
        }

        private void outArray(byte[] array)
        {
            string read_inf = "第一个扇区前十位数据是：";
            for (int i = 0; i < 10; i++)
            {
                int value = Convert.ToInt32(array[i]);
                string hexvalue = String.Format("{0:X}", value);
                read_inf = read_inf + " " + hexvalue;
            }
            ActivityLogTextBox.AppendText(read_inf + "\r\n");
        }
    }
    public class DriverLoader
    {
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;

        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;

        private const uint OPEN_EXISTING = 3;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeFileHandle CreateFileA(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);


        private System.IO.FileStream _DirverStream;
        private long _SectorLength = 0;
        private SafeFileHandle _DirverHandle;

        /// <summary>
        /// 扇区数
        /// </summary>
        public long SectorLength { get { return _SectorLength; } }


        /// <summary>
        /// 获取磁盘扇区信息
        /// </summary>
        /// <param name="DirverName">G:</param>
        public DriverLoader(string DirverName)
        {
            if (DirverName == null && DirverName.Trim().Length == 0) return;
            _DirverHandle = CreateFileA("\\\\.\\" + DirverName.Trim(), GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);

            _DirverStream = new System.IO.FileStream(_DirverHandle, System.IO.FileAccess.ReadWrite);

            GetSectorCount();
        }


        /// <summary>
        /// 扇区显示转换
        /// </summary>
        /// <param name="SectorBytes">扇区 长度512</param>
        /// <returns>EB 52 90 ......55 AA</returns>
        public string GetString(byte[] SectorBytes)
        {
            if (SectorBytes.Length != 512) return "";
            StringBuilder ReturnText = new StringBuilder();

            int RowCount = 0;
            for (int i = 0; i != 512; i++)
            {
                ReturnText.Append(SectorBytes[i].ToString("X02") + " ");

                if (RowCount == 15)
                {
                    ReturnText.Append("\r\n");
                    RowCount = -1;
                }

                RowCount++;
            }

            return ReturnText.ToString();

        }
        /// <summary>
        /// 获取扇区数
        /// </summary>
        private void GetSectorCount()
        {
            if (_DirverStream == null) return;
            _DirverStream.Position = 0;
            DriveInfo[] s = DriveInfo.GetDrives();
            foreach (DriveInfo drive in s)
            {

                if (drive.DriveType == DriveType.Removable)//获取U盘的方法
                {
                    //pf.Text = drive.Name.ToString();
                    break;
                }
            }
            ManagementClass Diskobject = new ManagementClass("Win32_DiskDrive");//获取一个磁盘实例对象
            ManagementObjectCollection moc = Diskobject.GetInstances();//获取对象信息的集合
            foreach (ManagementObject mo in moc)
            {
                if (mo.Properties["InterfaceType"].Value.ToString() == "USB")
                {
                    try
                    {
                        //产品名称
                        //Caption.Text = mo.Properties["Caption"].Value.ToString();

                        //总容量
                        string S = mo.Properties["Size"].Value.ToString();
                        long S_L = Convert.ToInt64(S);
                        S_L /= 512;
                        _SectorLength = S_L;

                        //Size.Text = mo.Properties["Size"].Value.ToString();
                        //Size.Text = S2;
                        /*
                        string[] info = mo.Properties["PNPDeviceID"].Value.ToString().Split('&');
                        string[] numbel = info[3].Split('\\');
                        //序列号
                        MessageBox.Show("U盘序列号:" + numbel[1]);
                        PNPDeviceID.Text = numbel[1];
                        numbel = numbel[0].Split('_');

                        //版本号
                        REV.Text = numbel[1];

                        //制造商ID
                        numbel = info[1].Split('_');
                        VID.Text = numbel[1];*/

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }

        }



        /// <summary>
        /// 读一个扇区
        /// </summary>
        /// <param name="SectorIndex">扇区号</param>
        /// <returns>如果扇区数字大于分区信息的扇区数 返回NULL</returns>
        public byte[] ReadSector(long SectorIndex)
        {
            if (SectorIndex > _SectorLength) return null;
            _DirverStream.Position = SectorIndex * 512;
            byte[] ReturnByte = new byte[512];
            _DirverStream.Read(ReturnByte, 0, 512); //获取扇区
            return ReturnByte;
        }
        /// <summary>
        /// 写入数据
        /// </summary>
        /// <param name="SectorBytes">扇区 长度512</param>
        /// <param name="SectorIndex">扇区位置</param>
        public void WritSector(byte[] SectorBytes, long SectorIndex)
        {
            if (SectorBytes.Length != 512) return;
            //if (SectorIndex > _SectorLength) return;

            _DirverStream.Position = SectorIndex * 512;
            _DirverStream.Write(SectorBytes, 0, 512); //写入扇区  
        }


        /// <summary>
        /// 关闭
        /// </summary>
        public void Close()
        {
            _DirverStream.Close();
        }

    }
}
