using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip;
using IcyTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Office.Interop.Excel;
using 批量提取json图片.Properties;
using MySql.Data.MySqlClient;
using System.Linq.Expressions;
using System.IO.Ports;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections.Concurrent;
using System.Drawing.Imaging;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Net.Mail;
using System.Net.Http;
using System.Net.Http.Headers;

namespace 批量提取json图片
{
    public partial class Form1:Form
    {
        public Form1()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
            string ip = GetLocalIp();
            Tbx_IP.Text = ip;
            Txb_SimulationIP.Text = ip;
            Txb_VehicleRec.Text = ip;
            Tbx_FindPlateFileDate.Text = "/home/data/Debug/Json/"+DateTime.Now.ToString("yyyy-MM-dd")+"/";
            dateTimePicker1.Value = DateTime.Now.Date;
            dateTimePicker2.Value = DateTime.Now.Date.AddDays(1);

            Save(Resources.IcyTools, "IcyTools.dll",true);
            Save(Resources.ICSharpCode_SharpZipLib, "ICSharpCode.SharpZipLib.dll");
            Save(Resources.microsoft_office_interop_excel, "microsoft.office.interop.excel.dll");
            Save(Resources.Newtonsoft_Json, "Newtonsoft.Json.dll");
            Save(Resources.Renci_SshNet, "Renci.SshNet.dll");
            Save(Resources.MySql_Data, "MySql.Data.dll");
        }



        #region 云端转发Retry/ETC/AI
        static AutoResetEvent myEvent = new AutoResetEvent(false);
        static int send_num = 0;
        public void RetryPost()
        {
            FileSystemInfo FirstFile = null;
            while (true)
            {
                try
                {
                    string filepath = "Json/Retry/";
                    var dir = new DirectoryInfo(filepath);
                    if (!dir.Exists)
                    {
                        dir.Create();
                    }
                    FileInfo[] fi = dir.GetFiles();
                    //int max= fi.Length>20?20:fi.Length;
                    if (fi.Length == 0)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                    //send_num = fi.Length > 30 ? 30 : fi.Length;//最大允许同时发起20个并发请求；防止大量数据堆积程序异常
                    //int thread_max = send_num;
                    //myEvent.Reset();
                    //for (int i = 0; i < thread_max; i++)
                    //{
                    //    FirstFile = fi[i];
                    //    //Send(FirstFile);
                    //    ThreadPool.QueueUserWorkItem(Send, FirstFile);
                    //}
                    //myEvent.WaitOne(5000);


                    for (int i = 0; i < fi.Length; i++)
                    {
                        FirstFile = fi[i];
                        Send(FirstFile);
                        Thread.Sleep(200);
                        //ThreadPool.QueueUserWorkItem(Send, FirstFile);
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message != "")
                        Log.WriteLog("RetryPost", "RetryPost", ex.Message);
                    Thread.Sleep(5000);
                }
            }
        }
        void Send(object name)
        {
            FileSystemInfo FirstFile = (FileSystemInfo)name;
            //string srcPath = "http://10.88.149.67:9950/";
            string srcPath = "http://localhost:2230/";
            string reContent = "";//http返回内容
            string JsonContent = File.ReadAllText(FirstFile.FullName);
            string aim = srcPath + FirstFile.Name.Substring(0, FirstFile.Name.Length-22)  + DateTime.Now.ToString("yyyyMMddHHmmssfff")+".json";//FirstFile.Name;
            reContent = IcyHttp.WebApiPost(aim, JsonContent);//云端总控
            if (!string.IsNullOrEmpty(reContent) && !reContent.StartsWith("Error"))
            {
                Log.WriteLog("RetryPost", "Send", FirstFile.Name);
                Accordtxt(reContent);
                File.Delete(FirstFile.FullName);
                lock (oj)
                {
                    txb_send_num.Text = (++index).ToString();
                }
            }
            else
            {
                Log.WriteLog("RetryPostError", "SendError", FirstFile.Name);
            }
        }

        //public void ETCPost()//读取Json/ETC/ 目录下文件，发送给云端总控，用于ETC路径还原
        //{
        //    string reContent = "";//http返回内容
        //    int index = 0;
        //    FileSystemInfo FirstFile = null;
        //    while (true)
        //    {
        //        try
        //        {
        //            string filepath = "Json/ETC/";
        //            var dir = new DirectoryInfo(filepath);
        //            if (!dir.Exists)
        //            {
        //                dir.Create();
        //            }
        //            FileInfo[] fi = dir.GetFiles();
        //            //Array.Sort(fi, delegate(FileInfo x, FileInfo y) { return y.CreationTime.CompareTo(x.CreationTime); });//按照创建时间，倒序输出
        //            for (int i = 0; i < fi.Length; i++)
        //            {
        //                FirstFile = fi[i];
        //                string JsonContent = File.ReadAllText(FirstFile.FullName);
        //                JObject obj = JObject.Parse(JsonContent);
        //                if ( obj["type"].ToString()=="B")
        //                {
        //                    obj["pic_base64"] = "";
        //                }
        //                JsonContent = JsonConvert.SerializeObject(obj);
        //                string URL = "http://10.102.1.46:8086/ly";
        //                //Log.WriteLog("RetryPostbefore", "Sendbefore", FirstFile.Name);
        //                reContent = IcyHttp.WebApiPost(URL, JsonContent);//匝道
        //                if (!string.IsNullOrEmpty(reContent) && !reContent.StartsWith("Error"))
        //                {
        //                    Log.WriteLog("ETCPost", "ETCPost", index++ + "    " + FirstFile.Name);
        //                    File.Delete(FirstFile.FullName);
        //                    //Thread.Sleep(50);
        //                }
        //            }
        //            Thread.Sleep(1000);
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.WriteLog("ETCPostError", "ETCPost", ex.Message);
        //            File.Delete(FirstFile.FullName);
        //        }
        //    }
        //}
        public void ETCPostParalel()
        {
            FileSystemInfo FirstFile = null;
            while (true)
            {
                try
                {
                    string filepath = "Json/ETC/";
                    var dir = new DirectoryInfo(filepath);
                    if (!dir.Exists)
                    {
                        dir.Create();
                    }
                    FileInfo[] fi = dir.GetFiles();
                    send_num = fi.Length > 30 ? 30 : fi.Length;//最大允许同时发起20个并发请求；防止大量数据堆积程序异常
                    int thread_max = send_num;
                    myEvent.Reset();
                    for (int i = 0; i < thread_max; i++)
                    {
                        FirstFile = fi[i];
                        //Send(FirstFile);
                        ThreadPool.QueueUserWorkItem(ETCSend, FirstFile);
                    }
                    myEvent.WaitOne(5000);
                }
                catch (Exception ex)
                {
                    Log.WriteLog("ETCPostError", "ETCPost", ex.Message);
                    File.Delete(FirstFile.FullName);
                }
            }
        }
        void ETCSend(object name)
        {
            FileSystemInfo FirstFile = (FileSystemInfo)name;
            try
            {
                string reContent = "";//http返回内容
                string JsonContent = File.ReadAllText(FirstFile.FullName);
                string aim = "http://10.102.1.46:8086/ly";
                reContent = IcyHttp.WebApiPost(aim, JsonContent);//云端总控
                if (!string.IsNullOrEmpty(reContent) && !reContent.StartsWith("Error"))
                {
                    Log.WriteLog("ETCPost", "Send", FirstFile.Name);
                    File.Delete(FirstFile.FullName);
                }
                else
                {
                    Log.WriteLog("ETCPostError", "SendError", FirstFile.Name);
                }
                lock (oj1)
                {
                    txb_send_num.Text = (int.Parse(txb_send_num.Text) + 1).ToString();
                    send_num--;
                }
                if (send_num == 0)
                {
                    myEvent.Set();
                }
            }
            catch (System.Exception ex)
            {
                Log.WriteLog("ETCPostError", "ETCPost", ex.Message);
                File.Delete(FirstFile.FullName);
            }

        }
        public void ETCPost()
        {
            string reContent = "";//http返回内容
            int index = 0;
            FileSystemInfo FirstFile = null;
            while (true)
            {
                try
                {
                    string filepath = "Json/ETC/";
                    var dir = new DirectoryInfo(filepath);
                    if (!dir.Exists)
                    {
                        dir.Create();
                    }
                    FileInfo[] fi = dir.GetFiles();
                    //Array.Sort(fi, delegate(FileInfo x, FileInfo y) { return y.CreationTime.CompareTo(x.CreationTime); });//按照创建时间，倒序输出
                    for (int i = 0; i < fi.Length; i++)
                    {
                        FirstFile = fi[i];
                        string JsonContent = File.ReadAllText(FirstFile.FullName);
                        JObject obj = JObject.Parse(JsonContent);
                        //if (obj.ContainsKey("type"))
                        //{
                        //    if (obj["type"].ToString() == "B")
                        //    {
                        //        obj["pic_base64"] = "";
                        //    }
                        //}
                        JsonContent = JsonConvert.SerializeObject(obj);
                        //string URL = "http://10.102.1.46:8086/ly";
                        //string URL = "http://10.100.8.49:8086/ly";
                        string URL = "http://106.120.201.126:14530/ly";
                        reContent = IcyHttp.WebApiPost(URL, JsonContent);//匝道
                        if (!string.IsNullOrEmpty(reContent) && !reContent.StartsWith("Error"))
                        {
                            Log.WriteLog("ETCPost", "ETCPost", index++ + "    " + FirstFile.Name);
                            txb_send_num.Text = index.ToString();
                            File.Delete(FirstFile.FullName);
                        }
                    }
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    Log.WriteLog("ETCPostError", "ETCPost", ex.Message);
                    File.Delete(FirstFile.FullName);
                }
            }
        }
        public void AIPost()
        {
            string reContent = "";//http返回内容
            int index = 0;
            FileSystemInfo FirstFile = null;
            while (true)
            {
                try
                {
                    string filepath = "Json/AI_SEND/";
                    var dir = new DirectoryInfo(filepath);
                    if (!dir.Exists)
                    {
                        dir.Create();
                    }
                    FileInfo[] fi = dir.GetFiles();
                    for (int i = 0; i < fi.Length; i++)
                    {
                        FirstFile = fi[i];
                        string JsonContent = File.ReadAllText(FirstFile.FullName);
                        string _url = "http://106.120.201.126:14531/v1/add_etc_info_to_cluster";
                        reContent = IcyHttp.WebApiPost(_url, JsonContent, 10);
                        if (!string.IsNullOrEmpty(reContent) && !reContent.StartsWith("Error"))
                        {
                            Log.WriteLog("AI_SEND", "AI_SEND", index++ + "    " + FirstFile.Name);
                            File.Delete(FirstFile.FullName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLog("AI_SEND_ERROR", "AI_SEND", ex.Message);
                }
            }
        }
        #endregion

        #region 辅助变量
        string path = "";
        int index = 0;
        object oj = new object();
        object oj1 = new object();
        object objrichbox = new object();
        public class EGlobal
        {
            public static string CloudPostFilePath = System.Environment.CurrentDirectory + "/Json/CloudPost/";//云端发送存储路径
            public static string EdgeUrl = "http://192.168.0.200:8218/v1/batch_detect_and_extract";//图像边缘机地址
        }

        #endregion


        #region 辅助功能
        private void Btn_Close_Click(object sender, EventArgs e)
        {
            kehuo_num = new int[10];
            ForeachFile2(Tbx_FindPlateFileDate.Text);
            Accordtxt(string.Join(",",kehuo_num));
            //KillProcess(new string[] { "Rename", "AutoPingServ" });
            //Environment.Exit(0);
        }
        int[] kehuo_num = new int[10];
        public void ForeachFile2(string filePathByForeach)
        {
            DirectoryInfo theFolder = new DirectoryInfo(filePathByForeach);
            DirectoryInfo[] dirInfo = theFolder.GetDirectories();//获取所在目录的文件夹
            FileInfo[] file = theFolder.GetFiles();//获取所在目录的文件

            string[] name = { "_客1.jpg", "_客2.jpg", "_客3.jpg", "_客4.jpg", "_货1.jpg", "_货2.jpg", "_货3.jpg", "_货4.jpg", "_货5.jpg", "_货6.jpg" };
            foreach (FileInfo fileItem in file) //遍历文件
            {
                for (int i = 0; i < name.Length; i++)
                {
                    if (fileItem.Name.Contains(name[i]))
                    {
                        kehuo_num[i]++;
                        break;
                    }
                }
            }
            //遍历文件夹
            foreach (DirectoryInfo NextFolder in dirInfo)
            {
                ForeachFile2(NextFolder.FullName);
            }
        }
        public static void KillProcess(string[] NameArr)
        {
            try
            {
                Process[] process = Process.GetProcesses();  
                for (int i = 0; i < NameArr.Length; i++)
                {
                    string Name = NameArr[i];
                    foreach (Process proces in process)
                    {
                        if (proces.ProcessName.Contains(Name))
                        {
                            proces.Kill();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLog("Error", "KillProcess", ex.ToString());
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Btn_ServerSimulate.Text == "服务器关闭")
            {
                DialogResult MsgBoxResult = MessageBox.Show("正在转发数据，请通知luoye，是否坚持关闭软件?", "警告", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (MsgBoxResult != DialogResult.Yes)
                {
                    e.Cancel = true;
                }
                else
                {
                    MessageBox.Show("你无法关闭此程序！！");
                    e.Cancel = true;
                }
            }
            else
            {
                KillProcess(new string[] { "Rename", "AutoPingServ" });
                Environment.Exit(0);
            }
        }
        public void Accordtxt(object str)
        {
            try
            {
                if (str != null)
                {
                    lock (objrichbox)
                    {
                        string _str = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + str + "\r\n";
                        this.richTextBox1.AppendText(_str);
                        Log.WriteLog("Accordtxt", "", _str);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString() + str);
            }
        }
        static void WriteJson(string sFilePath, string picTime, string content)
        {
            string localpath = System.Environment.CurrentDirectory + "/Json/" + picTime + "/";
            DateTime t1 = DateTime.Now;
            if (!Directory.Exists(localpath))//创建数据文件夹
            {
                Directory.CreateDirectory(localpath);
            }
            using (StreamWriter sw = new StreamWriter(localpath + sFilePath, true, System.Text.Encoding.UTF8))
            {
                sw.WriteLine(content);
                sw.Flush();
                sw.Dispose();
            }
            Log.WriteLog("WriteJson", "WriteJson", t1.ToString("yyyy-MM-dd HH:mm:ss.fff") + "    " + sFilePath);
        }
        public string GetLocalIp()
        {
            ///获取本地的IP地址
            string AddressIP = string.Empty;
            foreach (IPAddress _IPAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
                {
                    AddressIP = _IPAddress.ToString();
                    string subtemp = AddressIP.Substring(0, AddressIP.LastIndexOf("."));
                    if (subtemp == "192.168.1" || subtemp == "192.168.0")
                    {
                        break;
                    }
                }
            }
            return AddressIP;
        }
        private void Btn_CountPlus_Click(object sender, EventArgs e)
        {
            Txb_num.Text = (int.Parse(Txb_num.Text) + 1).ToString();
            Accordtxt(Txb_num.Text);
        }

        public static void dataTableToCsvT(System.Data.DataTable dt, string strFilePath)
        {
            if (dt == null || dt.Rows.Count == 0)   //确保DataTable中有数据
                return;
            string strBufferLine = "";
            StreamWriter strmWriterObj = new StreamWriter(strFilePath, false, System.Text.Encoding.Default);
            //写入列头
            foreach (System.Data.DataColumn col in dt.Columns)
                strBufferLine += col.ColumnName + ",";
            strBufferLine = strBufferLine.Substring(0, strBufferLine.Length - 1);
            strmWriterObj.WriteLine(strBufferLine);
            //写入记录
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                strBufferLine = "";
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    if (j > 0)
                    {
                        strBufferLine += ",";
                    }

                    strBufferLine += dt.Rows[i][j].ToString().Replace(",", "");   //因为CSV文件以逗号分割，在单元格内替换为空
                }
                strmWriterObj.WriteLine(strBufferLine);
            }
            strmWriterObj.Close();
        }
        void ExportCsv(System.Data.DataTable[] dtarr, string filename, string[] sheet_names)
        {
            for (int i = 0; i < dtarr.Length; i++)
            {
                dataTableToCsvT(dtarr[i], filename.Replace(".xlsx", "") + "_" + sheet_names[i] + ".csv");
            }
        }
        public static bool DataTable2Excel(System.Data.DataTable[] DTs, string FileName, string[] SheetNames)
        {
            if (DTs.Length != SheetNames.Length)
            {
                return false;
            }

            Microsoft.Office.Interop.Excel.Application xlApp = new Microsoft.Office.Interop.Excel.Application();
            if (xlApp == null)
            {
                //SetHint("可能机器未安装Excel！", HintType.Error);
                return false;
            }

            Microsoft.Office.Interop.Excel.Workbooks xlBooks = xlApp.Workbooks;
            Microsoft.Office.Interop.Excel.Workbook xlBook = xlBooks.Add(Microsoft.Office.Interop.Excel.XlWBATemplate.xlWBATWorksheet);
            //增加Sheet
            for (int iSht = 1; iSht < DTs.Length; iSht++)
            {
                xlBook.Sheets.Add();
            }

            xlApp.Visible = true;

            for (int iArr = 0; iArr < DTs.Length; iArr++)
            {
                Microsoft.Office.Interop.Excel.Worksheet xlSheet = (Microsoft.Office.Interop.Excel.Worksheet)xlBook.Worksheets[iArr + 1];
                //数据写数组
                object[,] objData = new object[DTs[iArr].Rows.Count + 1, DTs[iArr].Columns.Count];
                //--ColumnName
                for (int iDT = 0; iDT < DTs[iArr].Columns.Count; iDT++)
                {
                    objData[0, iDT] = DTs[iArr].Columns[iDT].ColumnName;
                }
                //--Data
                for (int iRow = 0; iRow < DTs[iArr].Rows.Count; iRow++)
                {
                    for (int iColumn = 0; iColumn < DTs[iArr].Columns.Count; iColumn++)
                    {
                        objData[iRow + 1, iColumn] = DTs[iArr].Rows[iRow][iColumn];
                    }
                }

                //Excel Column Name
                string startCol = "A";
                int iCnt = (DTs[iArr].Columns.Count / 26);
                string endColSignal = (iCnt == 0 ? "" : ((char)('A' + (iCnt - 1))).ToString());
                string endCol = endColSignal + ((char)('A' + DTs[iArr].Columns.Count - iCnt * 26 - 1)).ToString();

                Microsoft.Office.Interop.Excel.Range range = xlSheet.get_Range(
                    startCol + "1", endCol + (DTs[iArr].Rows.Count + 1).ToString()
                    );
                range.NumberFormatLocal = "G/通用格式";
                range.Value = objData;

                range.EntireColumn.AutoFit(); //设定Excel列宽度自适应  
                //Excel文件列名 字体设定为Bold  
                xlSheet.get_Range(startCol + "1", endCol + "1").Font.Bold = 1;
                xlSheet.Name = SheetNames[iArr];
            }

            xlApp.DisplayAlerts = false;
            xlApp.AlertBeforeOverwriting = false;
            xlBook.SaveAs(FileName);
            //xlApp.Quit();

            return true;
        }




        //Dialog封装 
        public delegate void DialogFunction(OpenFileDialog dialog);
        void DialogSelect(string file_filter, DialogFunction dosomething)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.InitialDirectory = System.Environment.CurrentDirectory;
                dialog.Filter = file_filter;
                dialog.ValidateNames = true;
                dialog.CheckPathExists = true;
                dialog.CheckFileExists = true;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    dosomething(dialog);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        public void Save(byte[] bt, string FilePath)
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    return;
                }
                if (!Directory.Exists(Directory.GetParent(FilePath).FullName))
                    Directory.CreateDirectory(Directory.GetParent(FilePath).FullName);
                using (FileStream pFileStream = new FileStream(FilePath, FileMode.OpenOrCreate))
                {
                    pFileStream.Write(bt, 0, bt.Length);
                }
            }
            catch (System.Exception ex)
            {
                Accordtxt(ex.ToString());
            }
        }
        public void Save(byte[] bt, string FilePath, bool DeleteFlag)
        {
            if (DeleteFlag && File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
            Save(bt, FilePath);
        }

        private void richTextBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                richTextBox1.Text = "";
            }
        }
        #endregion


        #region 服务器开启
        private void Btn_ServerSimulate_Click(object sender, EventArgs e)
        {
            try
            {
                if (Btn_ServerSimulate.Text == "服务器开启")
                {
                    IcyHttpServer.DataReceivedEvent = new IcyHttpServer.DataReceived(DataReceivedHandle);
                    Dictionary<int, bool> serverPara = new Dictionary<int, bool>();
                    int port = int.Parse(Tbx_port.Text);
                    serverPara.Add(port, false);
                    if (IcyHttpServer.Start(serverPara) == 0)
                    {
                        Accordtxt("模拟服务器开启，端口号为：" + port);
                        Btn_ServerSimulate.Text = "服务器关闭";
                    }
                    else
                    {
                        Accordtxt("模拟服务器开启失败");
                        IcyHttpServer.Stop();
                        Btn_ServerSimulate.Text = "服务器开启";
                    }
                    //if (first_time)
                    //{
                    //    IcyThread.Start(ETCPost);//读取json发送给ETC服务器
                    //    IcyThread.Start(AIPost);//读取json发送给AI服务器
                    //    IcyThread.Start(RetryPost);//读取json发送给AI服务器
                    //    first_time = false;
                    //}
                }
                else
                {
                    int ret = IcyHttpServer.Stop();
                    Btn_ServerSimulate.Text = "服务器开启";
                    Accordtxt(ret + "模拟服务器关闭");
                }
            }
            catch (System.Exception ex)
            {
                Accordtxt(ex.ToString());
            }
        }
        void DataReceivedHandle(HttpListenerRequest request, HttpListenerResponse response, NameValueCollection nvc)
        {
            try
            {
                //txb_receive_num.Text = (int.Parse(txb_receive_num.Text) + 1).ToString();
                string absPath = request.Url.AbsolutePath.Substring(1);
                int port = request.Url.Port;
                if (port == 2230)
                {
                    dealCamera(request, response, absPath);
                }
                else if (port == 6666)
                {
                    //using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
                    //{
                    //    string body = reader.ReadToEnd();
                    //    Accordtxt(body + "\t" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss.fff"));
                    //}
                    Dictionary<string, object> reBizC = new Dictionary<string, object>();
                    reBizC.Add("code", 707);
                    reBizC.Add("error", "");
                    reBizC.Add("status", "OK");
                    IcyHttpServer.StreamWrite(response.OutputStream, IcyJson.ToJsonStr(reBizC), Encoding.UTF8);
                }
                else if (port == 8084)
                {
                    string fileName = System.Environment.CurrentDirectory + "/Json/response.json";
                    string content = File.ReadAllText(fileName);
                    IcyHttpServer.StreamWrite(response.OutputStream, content, Encoding.UTF8);
                }
                else if (port == 8086)
                {
                    if (absPath == "ly")
                    {
                        //txb_receive_num.Text = (int.Parse(txb_receive_num.Text) + 1).ToString();
                        //转发到服务器
                        using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
                        {
                            string body = reader.ReadToEnd();
                            string filename = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fff") + ".json";

                            WriteJson(filename, "ETC", body);


                            Dictionary<string, object> reBizC = new Dictionary<string, object>();
                            reBizC.Add("code", 0);
                            reBizC.Add("error", "");
                            reBizC.Add("status", "OK");
                            IcyHttpServer.StreamWrite(response.OutputStream, IcyJson.ToJsonStr(reBizC), Encoding.UTF8);
                        }
                    }
                    else if (absPath.StartsWith("watchbegin"))
                    {
                        using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
                        {
                            string body = reader.ReadToEnd();
                            string reContent = IcyHttp.WebApiPost("http://106.120.201.126:14532/watchbegin/", body);//云端总控
                            IcyHttpServer.StreamWrite(response.OutputStream, reContent, Encoding.UTF8);
                        }
                    }
                    else if (absPath.StartsWith("wgcloud"))
                    {
                        using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
                        {
                            string body = reader.ReadToEnd();
                            string reContent = IcyHttp.WebApiPost("http://106.120.201.126:14532/"+absPath, body);//云端总控
                            IcyHttpServer.StreamWrite(response.OutputStream, reContent, Encoding.UTF8);
                        }
                    }
                    else if (absPath == "AI")
                    {
                        //txb_receive_num.Text = (int.Parse(txb_receive_num.Text) + 1).ToString();
                        //转发到服务器
                        using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
                        {
                            string body = reader.ReadToEnd();
                            string filename = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fff") + ".json";
                            WriteJson(filename, "AI_SEND", body);
                            //WriteJson(filename, "AI/" + DateTime.Now.ToString("yyyy-MM-dd"), body);
                            Dictionary<string, object> reBizC = new Dictionary<string, object>();
                            reBizC.Add("code", 0);
                            reBizC.Add("error", "");
                            reBizC.Add("status", "OK");
                            IcyHttpServer.StreamWrite(response.OutputStream, IcyJson.ToJsonStr(reBizC), Encoding.UTF8);
                        }
                    }
                    else
                    {
                        using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
                        {
                            string body = reader.ReadToEnd();
                            string filename = absPath;
                            WriteJson(filename, "Retry", body);
                            Dictionary<string, object> reBizC = new Dictionary<string, object>();
                            reBizC.Add("code", 0);
                            reBizC.Add("error", "");
                            reBizC.Add("status", "OK");
                            IcyHttpServer.StreamWrite(response.OutputStream, IcyJson.ToJsonStr(reBizC), Encoding.UTF8);
                        }
                    }
                }
                else if (port==8989)
                {
                    using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        string filename = absPath.Split('/')[0];
                        Log.WriteLog(filename, absPath, body);
                    }
                    string jsonName = absPath.Split('/')[absPath.Split('/').Length - 1];
                    if (jsonName.StartsWith("LaserSyn"))
                    {
                        DateTime jsontime = IcyCvt.ToDateTime(jsonName.Substring(jsonName.IndexOf('_') + 1, 17), "yyyyMMddHHmmssfff");
                        string laserSyn = "激光同步时间推送" + jsonName.Substring(jsonName.IndexOf('_') + 1, 17) + "  时间差:" + (DateTime.Now - jsontime).Milliseconds;
                        Log.WriteLog("LaserSyn", "LaserSyn", laserSyn);
                    }
                    Dictionary<string, object> back = new Dictionary<string, object>();
                    back.Add("message", "OK");
                    back.Add("receiveTime", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
                    SendBack(response, IcyJson.ToJsonStr(back));
                }
                else if (port == 9999)
                {
                    string URL = "http://39.103.130.180:8080/avi/device/1000000000000004";
                    using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        string reContent = IcyHttp.WebApiPost(URL, body, 30);
                        Log.WriteLog("Post", "reContent", reContent);
                        if (checkBox_savedata.Checked)
                        {
                            string filename = DateTime.Now.ToString("yyyy-MM-ddTHHmmssfff") + ".json";
                            WriteJson(filename, DateTime.Now.ToString("yyyy-MM-dd/HH"), body);
                        }
                        lock (oj1)
                        {
                            txb_send_num.Text = (int.Parse(txb_send_num.Text) + 1).ToString();
                        }
                        SendBack(response, reContent);
                    }

                    //using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
                    //{
                    //    string body = reader.ReadToEnd();
                    //    string filename = DateTime.Now.ToString("yyyy-MM-ddTHHmmssfff")+".json";
                    //    WriteJson(filename, DateTime.Now.ToString("yyyy-MM-dd/HH"), body);
                    //}
                    //Dictionary<string, object> back = new Dictionary<string, object>();
                    //back.Add("code", "0");
                    //back.Add("message", "OK");
                    //back.Add("receiveTime", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
                    //
                }
                else if (port == 10000)
                {
                    //byte[] buffer = File.ReadAllBytes("D:/2.jpg");
                    byte[] buffer = File.ReadAllBytes("D:/1.json");
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                Accordtxt(ex.ToString());
            }
        }

        static void SendBack(HttpListenerResponse response, string reContent)
        {
            SendBack(response, reContent, true);
        }
        static void SendBack(HttpListenerResponse response, string reContent, bool SignOk)
        {
            string backStr = reContent;
            byte[] backBytes = Encoding.UTF8.GetBytes(backStr);
            response.ContentLength64 = backBytes.Length;
            using (Stream writer = response.OutputStream)
            {
                writer.Write(backBytes, 0, backBytes.Length);
            }
        }
        #endregion


        #region 相机处理
        void dealCamera(HttpListenerRequest request, HttpListenerResponse response, string absPath)
        {
            string[] absPaths = absPath.Split('/'); //请求路径解析
            string jsonName = absPaths[absPaths.Length - 1];
            int recvType = getRecvType(jsonName);

            string reContent = "";  //返回内容
            Dictionary<string, object> reBizC = new Dictionary<string, object>();

            if (recvType == 21 || recvType == 22 || recvType == 26)//相机直连初始化/基础数据上传/状态信息上传
            {
                using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
                {
                    string body = reader.ReadToEnd();
                    string filename = jsonName;
                    WriteJson(filename, DateTime.Now.ToString("yyyy-MM-dd"), body);
                }
                //直接回复
                string showstr = recvType == 21 ? "初始化" : recvType == 22 ? "基础数据" : recvType == 26 ? "状态信息" : "Unknow";
                Accordtxt(showstr);
                if (recvType == 21)
                {
                    if (!Cbx_Ini.Checked)
                    {
                        return;
                    }
                    Thread.Sleep(int.Parse(Tbx_DelayIni.Text.Trim()));
                    reBizC.Add("gantryHex", "2CFFFF");//ETC门架Hex值 String 如：2CFFFF 
                    reBizC.Add("gantryOrderNum", 1);//门架顺序号 Integer 1：行车方向第一排门架 2：行车方向第二排门架 3：行车方向第三排门架
                    reBizC.Add("driveDir", 1);//行驶方向 Integer 1-上行 2-下行
                    reBizC.Add("receiveTime", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));//服务端接收时间 String YYYY-MM-DDTHH:mm:ss
                    reBizC.Add("subCode", 0);//错误码 Integer 详见附录A.4 
                    reBizC.Add("errorMsg", "成功");//错误描述 String 如：成功
                    reContent = IcyJson.ToJsonStr(reBizC);
                }
                else
                {
                    reBizC.Add("receiveTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));//服务端接收时间 String YYYY-MM-DDTHH:mm:ss
                    reBizC.Add("subCode", 0);//错误码 Integer 详见附录A.4 
                    reBizC.Add("errorMsg", "成功");//错误描述 String 如：成功
                    reContent = IcyJson.ToJsonStr(reBizC);
                }
                PackToCamera(response, reContent);
            }
            else if (recvType == 23 || recvType == 25)//相机直连 23图片流水上传  25 图片上传 写入
            {
                //Thread.Sleep(20000);
                if (recvType == 23)
                {
                    if (!Cbx_PicInfo.Checked)
                    {
                        return;
                    }
                    Thread.Sleep(int.Parse(Tbx_DelayPicInfo.Text.Trim()));
                }
                else
                {
                    if (!Cbx_Pic.Checked)
                    {
                        return;
                    }
                    Thread.Sleep(int.Parse(Tbx_DelayPic.Text.Trim()));
                }
                using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
                {
                    string body = reader.ReadToEnd();
                    JObject obj = JObject.Parse(body);
                    JObject bizContent = JObject.Parse(obj["bizContent"].ToString());
                    string picid = bizContent["picInfoList"][0]["picId"].ToString();
                    string pictime = Convert.ToDateTime(bizContent["picInfoList"][0]["picTime"]).ToString("yyyy-MM-dd/HH");
                    string filename = jsonName;
                    WriteJson(filename, pictime, body);
                    Accordtxt(recvType == 23 ? "图片流水上传" + picid : "图片上传" + picid);
                }
                reBizC.Add("receiveTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));//服务端接收时间 String YYYY-MM-DDTHH:mm:ss
                reBizC.Add("subCode", 0);//错误码 Integer 详见附录A.4 
                reBizC.Add("errorMsg", "成功");//错误描述 String 如：成功
                reContent = IcyJson.ToJsonStr(reBizC);

                PackToCamera(response, reContent);
            }
            else if (recvType == 2)
            {
                if (!Cbx_Location.Checked)
                {
                    return;
                }
                Thread.Sleep(int.Parse(Tbx_DelayLocation.Text.Trim()));
                //保存json
                using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
                {
                    string body = reader.ReadToEnd();
                    JObject obj = JObject.Parse(body);
                    JObject JsonObj = JObject.Parse(obj["bizContent"].ToString());
                    var json = JsonObj["LocationInfoList"][0];
                    string picid = json["picId"].ToString();
                    Accordtxt("位置信息上传" + picid);
                    string pictime = Convert.ToDateTime(json["picTime"]).ToString("yyyy-MM-dd/HH");
                    string filename = picid + "_" + jsonName;
                    WriteJson(filename, pictime, body);
                }

                reBizC.Add("receiveTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));//服务端接收时间 String YYYY-MM-DDTHH:mm:ss
                reBizC.Add("subCode", 0);//错误码 Integer 详见附录A.4 
                reBizC.Add("errorMsg", "成功");//错误描述 String 如：成功
                reContent = IcyJson.ToJsonStr(reBizC);
                PackToCamera(response, reContent);

            }
            else
            {
                reBizC.Add("receiveTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));//服务端接收时间 String YYYY-MM-DDTHH:mm:ss
                reBizC.Add("subCode", 0);//错误码 Integer 详见附录A.4 
                reBizC.Add("errorMsg", "成功");//错误描述 String 如：成功
                reContent = IcyJson.ToJsonStr(reBizC);
                PackToCamera(response, reContent);
            }
        }



        static string makeSign(Dictionary<string, object> back, string key)
        {
            //----------------------新规
            //第一步：对参数按照 key=value 的格式，并按照参数名 ASCII 字典序排序; 不包含空值
            string stringA = IcyHttpServer.formatParameters(back, false);
            //第二步：stringA 最后拼接上 filename 名称得到 stringSignTemp
            string stringSignTemp = stringA + (key == "" ? "" : "&filename=" + key);
            //第三步：MD5 生成待签名串
            string sign = IcyEncrypt.GetMD5_Bytes(stringSignTemp).ToUpper();
            return sign;
        }
        static Dictionary<string, object> makeSignAndPackBack(KeyValuePair<int, string> GateResponseType, string reContent)
        {
            Dictionary<string, object> back = new Dictionary<string, object>();
            back.Add("statusCode", GateResponseType.Key);
            back.Add("errorMsg", GateResponseType.Value);
            back.Add("bizContent", reContent);
            back.Add("sign", makeSign(back, ""));
            return back;
        }
        class RsuGateResponseType
        {
            public static KeyValuePair<int, string> Success = new KeyValuePair<int, string>(10000, "网关调用成功");
            public static KeyValuePair<int, string> GateFail = new KeyValuePair<int, string>(40000, "网关调用失败");
            public static KeyValuePair<int, string> AuthLack = new KeyValuePair<int, string>(40001, "授权权限不足");
            public static KeyValuePair<int, string> ParaLack = new KeyValuePair<int, string>(90001, "缺少必选参数");
            public static KeyValuePair<int, string> ParaError = new KeyValuePair<int, string>(90002, "非法的参数");
            public static KeyValuePair<int, string> DealFail = new KeyValuePair<int, string>(90004, "业务处理失败");
        }
        static void PackToCamera(HttpListenerResponse response, string reContent)
        {
            Dictionary<string, object> back = makeSignAndPackBack(RsuGateResponseType.Success, reContent);
            reContent = IcyJson.ToJsonStr(back);
           SendBack(response, reContent);
        }
        static int getRecvType(string jsonName)
        {
            //-----------------------------------------------------------------
            if (jsonName.StartsWith("TPM_PUTTRANS"))//3.3	交易定位推送
                return 1;
            else if (jsonName.StartsWith("TPM_PUTPICTURE"))//3.4	图片推送
                return 2;
            else if (jsonName.StartsWith("LPR_LOCATIONJOURUPLOAD"))//3.4	图片推送
                return 2;
            else if (jsonName.StartsWith("TPM_PUTLASERLOCATION"))//3.6	激光定位推送
                return 3;
            else if (jsonName.StartsWith("TPM_GETRESULT"))////3.1	获取匹配结果
                return 4;

            //-----------------------------------------------------------------
            else if (jsonName.StartsWith("RSU_BASEINFOUPLOAD"))//RSU基础信息
                return 11;
            else if (jsonName.StartsWith("RSU_STATUSUPLOAD"))//RSU设备状态
                return 12;
            else if (jsonName.StartsWith("RSU_UPDATE"))//RSU升级
                return 13;

            //-----------------------------------------------------------------
            else if (jsonName.Contains("INIT_VIUINIT_REQ"))//相机直连初始化
                return 21;
            else if (jsonName.Contains("MON_BVIUBASEINFO_REQ"))//相机直连 基础数据上传
                return 22;
            else if (jsonName.Contains("TRC_BVIU_REQ"))//相机直连 图片流水上传
                return 23;
            else if (jsonName.Contains("LPR_LOCATIONJOURUPLOAD_REQ"))//相机直连 位置检测 //return 2;和上面一致
                return 2;
            else if (jsonName.Contains("TRC_BVIPU_REQ"))//相机直连 图片上传
                return 25;
            else if (jsonName.Contains("MON_BVIUSTATE_REQ"))//相机直连 状态信息上传
                return 26;

            //-----------------------------------------------------------------
            else if (jsonName.StartsWith("TPM_LASERHEARTBEAT"))//激光心跳数据
                return 31;
            else if (jsonName.StartsWith("LaserSynTime"))//激光时间同步
                return 32;
            else if (jsonName.StartsWith("LaserTrigChg"))//激光时间同步
                return 33;
            else return -1;
        }
        #endregion


        #region 提取文件
        List<Task> task_list = new List<Task>();
        private void Btn_GetFromFile_Click(object sender, EventArgs e) //从文件夹提取
        {
            try
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                dialog.Description = "请选择文件路径";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string str = dialog.SelectedPath;
                    task_list.Clear();
                    DateTime t1 = DateTime.Now;
                    ForeachFile(str);
                    Task.WaitAll(task_list.ToArray());
                    DateTime t2 = DateTime.Now;
                    Accordtxt((t2 - t1).TotalMilliseconds.ToString() + " ms");
                    Accordtxt(task_list.Count + "个Json提取完成，请在照片目录下查看");
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        public void ForeachFile(string filePathByForeach)
        {
            DirectoryInfo theFolder = new DirectoryInfo(filePathByForeach);
            DirectoryInfo[] dirInfo = theFolder.GetDirectories();//获取所在目录的文件夹
            FileInfo[] file = theFolder.GetFiles();//获取所在目录的文件
            string fileName = "";

            foreach (FileInfo fileItem in file) //遍历文件
            {
                try
                {
                    fileName = fileItem.FullName;
                    if (fileName.Contains("TRC_BVIPU_REQ") || fileName.Contains("PROOFPICTURE"))
                    {
                        if (checkBox1.Checked)
                        {
                            string str = Path.GetFileName(fileName).Split('_')[2];
                            if (Convert.ToInt32(str) < 256)
                            {
                                continue;
                            }
                        }
                        task_list.Add(Task.Factory.StartNew(PicCatch, fileName));
                        //ThreadPool.QueueUserWorkItem(PicCatch, fileName);
                    }
                }
                catch (System.Exception ex)
                {
                    Log.WriteLog("Error", "ForeachFile", fileName + "\n" + ex.ToString());
                }
            }
            //遍历文件夹
            foreach (DirectoryInfo NextFolder in dirInfo)
            {
                ForeachFile(NextFolder.FullName);
            }
        }
        private void Btn_OpenJson_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                if (path == "")
                {
                    dialog.InitialDirectory = System.Environment.CurrentDirectory + "/";
                }
                else
                {
                    dialog.InitialDirectory = path;
                }
                dialog.Filter = "文件(*.json)|*.json|所有文件|*.*";
                dialog.ValidateNames = true;
                dialog.CheckPathExists = true;
                dialog.CheckFileExists = true;
                dialog.Multiselect = true;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    path = dialog.FileNames[0];
                    string fileName = "";
                    task_list.Clear();
                    DateTime t1 = DateTime.Now;
                    for (int i = 0; i < dialog.FileNames.Length; i++)
                    {

                        fileName = dialog.FileNames[i];
                        try
                        {
                            if (fileName.Contains("TRC_BVIPU_REQ") || fileName.Contains("PROOFPICTURE"))
                            {
                                if (checkBox1.Checked)
                                {
                                    string str = Path.GetFileName(fileName).Split('_')[2];
                                    if (Convert.ToInt32(str) < 256)
                                    {
                                        continue;
                                    }
                                }
                                task_list.Add(Task.Factory.StartNew(PicCatch, fileName));
                                //ThreadPool.QueueUserWorkItem(PicCatch, fileName);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Accordtxt(fileName + "\n" + ex.ToString());
                        }
                    }
                    Task.WaitAll(task_list.ToArray());
                    DateTime t2 = DateTime.Now;
                    Accordtxt((t2 - t1).TotalMilliseconds.ToString() + "ms");
                    Accordtxt(task_list.Count + " 个Json提取完成，请在照片目录下查看！");
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        void PicCatch(object filename)
        {
            try
            {
                string fileName = (string)filename;
                string content = File.ReadAllText(fileName);
                JObject obj = JObject.Parse(content);
                string Img = "";
                string licImg = "";
                
                if (obj.ContainsKey("proofImage"))
                {
                    Img = obj["proofImage"].ToString();
                }
                else
                {
                    JObject picObj = JObject.Parse(obj["bizContent"].ToString());
                    Img = picObj["picInfoList"][0]["image"].ToString();
                    licImg = picObj["picInfoList"][0]["licenseImage"].ToString();
                }
               
                string tempfileName = Path.GetFileName(fileName);
                string directoryfilename = Path.GetDirectoryName(fileName);
                string filepath = tempfileName.Replace("json", "jpg");//tempfileName.Substring(0, tempfileName.IndexOf("TRC") - 1) + ".jpg";
                if (Img != "")
                {
                    SavePic(Img, directoryfilename, filepath);
                }
                filepath = filepath.Replace(".jpg", "plate.jpg");
                if (licImg != "")
                {
                    SavePic(licImg, directoryfilename, filepath);
                }
            }
            catch (System.Exception ex)
            {
                Log.WriteLog("Error", "PicCatch", ex.ToString());
            }
        }
        public void SavePic(string image, string localpath, string filepath)
        {
            //localpath = System.Environment.CurrentDirectory + "/照片/" + DateTime.Now.ToString("yyyy-MM-dd-HH") + "/";
            if (!Directory.Exists(localpath))//创建数据文件夹
            {
                Directory.CreateDirectory(localpath);
            }
            var bytes = Convert.FromBase64String(image);
            using (var imageFile = new FileStream(localpath + "/" + filepath, FileMode.Create))
            {
                imageFile.Write(bytes, 0, bytes.Length);
                imageFile.Flush();
            }
        }
        #endregion


        #region 边缘机测试
        private void Btn_EdgeTest_Click(object sender, EventArgs e)//边缘机测试
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (path == "")
            {
                dialog.InitialDirectory = System.Environment.CurrentDirectory;
            }
            else
            {
                dialog.InitialDirectory = path;
            }
            dialog.Filter = "文件(*.jpg)|*.jpg|所有文件|*.*";
            dialog.Multiselect = true;
            dialog.ValidateNames = true;
            dialog.CheckPathExists = true;
            dialog.CheckFileExists = true;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                path = dialog.FileNames[0];
                string fileName = "";
                int sendindex = 0, rcvindex = 0;
                string reContent;
                double TotalTime = 0;
                int ErrorCount = 0;
                string ErrorFilePath = "Json/error/";
                if (!Directory.Exists(ErrorFilePath))//创建数据文件夹
                {
                    Directory.CreateDirectory(ErrorFilePath);
                }
                string TrueFilePath = "Json/true/";
                if (!Directory.Exists(TrueFilePath))//创建数据文件夹
                {
                    Directory.CreateDirectory(TrueFilePath);
                }
                int interval = int.Parse(Txb_SendInterval.Text);
                for (int j = 0; j < int.Parse(Txb_LoopTimes.Text); j++)
                {
                    for (int i = 0; i < dialog.FileNames.Length; i++)
                    {
                        try
                        {
                            fileName = dialog.FileNames[i];
                            string jpgstr = Convert.ToBase64String(IcyFile.getBytes(fileName));
                            string JsonSendToEdge = EdgeAIJsonPack(jpgstr);
                            //Log.WriteLog("EdgePost", "SendToEdge", "发送：" + sendindex + "接收：" + rcvindex + Path.GetFileName(fileName) + "\n" + JsonSendToEdge);
                            sendindex++;
                            DateTime t1 = DateTime.Now;
                            reContent = IcyHttp.WebApiPost(EGlobal.EdgeUrl, JsonSendToEdge);//发送给边缘图像机
                            DateTime t2 = DateTime.Now;
                            if (!string.IsNullOrEmpty(reContent) && !reContent.StartsWith("Error"))
                            {
                                rcvindex++;
                                TotalTime += (t2 - t1).TotalMilliseconds;
                                Log.WriteLog("EdgePost", "SendToEdge", "发送：" + sendindex + "接收：" + rcvindex + "耗时：" + (t2 - t1).TotalMilliseconds + "文件名：" + Path.GetFileName(fileName) + "\n回复内容：" + reContent);
                                JObject ReObj = JObject.Parse(reContent);
                                if (ReObj["responses"][0]["objects"].Count() == 0)//图像边缘机返回正常
                                {
                                    string copyfile = ErrorFilePath + Path.GetFileName(fileName);

                                    File.Copy(fileName, copyfile, true);
                                    ErrorCount++;
                                }
                                else
                                {
                                    string copyfile = TrueFilePath + Path.GetFileName(fileName);

                                    File.Copy(fileName, copyfile, true);
                                }
                            }
                            Thread.Sleep(interval);
                        }
                        catch (System.Exception ex)
                        {
                            Log.WriteLog("Error", "JpgToSt", ex.ToString());
                        }
                    }
                }
                Accordtxt("测试完成");
                Accordtxt("总发送数：" + sendindex + "总接收数：" + rcvindex + "平均耗时(ms)：" + TotalTime / sendindex + "未识别车辆数：" + ErrorCount);
            }
        }
        static string EdgeAIJsonPack(string picstring)
        {
            var treeView = new TreeView();
            var childrenTree = new TreeChildrenView();
            var chchTree = new Tree2ChildrenView()
            {
                data = picstring
            };
            childrenTree.image = chchTree;
            treeView.requests = new List<TreeChildrenView>();
            treeView.requests.Add(childrenTree);
            return JsonConvert.SerializeObject(treeView);
        }
        // 多层封装json  格式为{"requests":[{"image":{"data":"abcd"}}]}
        public class TreeView
        {
            public IList<TreeChildrenView> requests { get; set; }
        }
        public class TreeChildrenView
        {
            public Tree2ChildrenView image { get; set; }
        }
        public class Tree2ChildrenView
        {
            public string data { get; set; }
        }
        #endregion


        #region 压缩
        private void Btn_Zip_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择文件路径";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string strfile = dialog.SelectedPath;
                string strzip = strfile + ".zip";
                ZipFile(strfile, strzip);
            }
            Accordtxt("压缩完成");
        }
        public void ZipFile(string strFile, string strZip)
        {
            if (strFile[strFile.Length - 1] != Path.DirectorySeparatorChar)
            {
                strFile += Path.DirectorySeparatorChar;
            }
            ZipOutputStream outstream = new ZipOutputStream(File.Create(strZip));
            outstream.SetLevel(6);
            ZipCompress(strFile, outstream, strFile);
            outstream.Finish();
            outstream.Close();
            Accordtxt(Path.GetFileName(strZip) + "压缩成功！");
        }
        public void ZipCompress(string strFile, ZipOutputStream outstream, string staticFile)
        {
            if (strFile[strFile.Length - 1] != Path.DirectorySeparatorChar)
            {
                strFile += Path.DirectorySeparatorChar;
            }
            Crc32 crc = new Crc32();
            //获取指定目录下所有文件和子目录文件名称
            string[] filenames = Directory.GetFileSystemEntries(strFile);
            //遍历文件
            foreach (string file in filenames)
            {
                if (Directory.Exists(file))
                {
                    ZipCompress(file, outstream, staticFile);
                }
                //否则，直接压缩文件
                else
                {
                    //打开文件
                    FileStream fs = File.OpenRead(file);
                    //定义缓存区对象
                    byte[] buffer = new byte[fs.Length];
                    //通过字符流，读取文件
                    fs.Read(buffer, 0, buffer.Length);
                    //得到目录下的文件（比如:D:\Debug1\test）,test
                    string tempfile = file.Substring(staticFile.LastIndexOf("\\") + 1);
                    ZipEntry entry = new ZipEntry(tempfile);
                    entry.DateTime = DateTime.Now;
                    entry.Size = fs.Length;
                    fs.Close();
                    crc.Reset();
                    crc.Update(buffer);
                    entry.Crc = crc.Value;
                    outstream.PutNextEntry(entry);
                    //写文件
                    outstream.Write(buffer, 0, buffer.Length);
                }
            }
        }



        public static void UnZip(Stream ZipFile, string TargetDirectory, string Password, bool OverWrite = true)
        {
            //如果解压到的目录不存在，则报错
            if (!System.IO.Directory.Exists(TargetDirectory))
            {
                throw new System.IO.FileNotFoundException("指定的目录: " + TargetDirectory + " 不存在!");
            }
            //目录结尾
            if (!TargetDirectory.EndsWith("\\")) { TargetDirectory = TargetDirectory + "\\"; }

            using (ZipInputStream zipfiles = new ZipInputStream(ZipFile))
            {
                zipfiles.Password = Password;
                ZipEntry theEntry;

                while ((theEntry = zipfiles.GetNextEntry()) != null)
                {
                    string directoryName = "";
                    string pathToZip = "";
                    pathToZip = theEntry.Name;

                    if (pathToZip != "")
                        directoryName = Path.GetDirectoryName(pathToZip) + "\\";

                    string fileName = Path.GetFileName(pathToZip);

                    Directory.CreateDirectory(TargetDirectory + directoryName);

                    if (fileName != "")
                    {
                        if ((File.Exists(TargetDirectory + directoryName + fileName) && OverWrite) || (!File.Exists(TargetDirectory + directoryName + fileName)))
                        {
                            using (FileStream streamWriter = File.Create(TargetDirectory + directoryName + fileName))
                            {
                                int size = 2048;
                                byte[] data = new byte[2048];
                                while (true)
                                {
                                    size = zipfiles.Read(data, 0, data.Length);
                                    if (size > 0)
                                        streamWriter.Write(data, 0, size);
                                    else
                                        break;
                                }
                                streamWriter.Close();
                            }
                        }
                    }
                }

                zipfiles.Close();
            }
        }
        #endregion

        #region 机柜转发

        private void Btn_Transfer_Click(object sender, EventArgs e)
        {
            IcyThread.Start(transfer);
        }
        public void transfer()
        {
            bool isContinue = true;
            int i = 0;
            IcyTCPserver.Init(9000);
            while (isContinue)
            {
                if (!IcyTCP.Init("192.168.0.233", 9000))
                {
                    Thread.Sleep(i * 1000);
                    Accordtxt("机柜服务器连接失败，尝试重连");
                }
                else
                {
                    isContinue = false;
                    IcyTCP.DataReceivedEvent = new IcyTCP.DataReceived(TCP_DataReceivedHandle);
                    Accordtxt("机柜服务器连接成功，开启转发");
                }
            }
        }
        static void TCP_DataReceivedHandle(byte[] l_RecvBuf)
        {
            string temp = "";
            for (int i = 0; i < l_RecvBuf.Length; i++)
            {
                temp += l_RecvBuf[i].ToString("X2") + " ";
            }
            IcyConsole.WriteLine(temp);
            IcyTCPserver.Send(IcyTCPserver.clients.FirstOrDefault().Value, l_RecvBuf);
        }
        #endregion


        #region 云端统计操作
        private void Btn_MultiCal_Click(object sender, EventArgs e)//多门架数据分析
        {
            bool connStatus = DBService.testCon("MYSQL", "127.0.0.1", "root", "12345", "ElectTollData", 5);
            if (!connStatus)
            {
                Accordtxt("数据库连接失败！");
                return;
            }
            DateTime start1 = Convert.ToDateTime(dateTimePicker1.Text);
            DateTime end1 = Convert.ToDateTime(dateTimePicker2.Text);
            for (int i = 0; i < (end1 - start1).Days; i++)
            {
                DateTime start = start1.AddDays(i);
                DateTime end = start.AddDays(1);
                System.Data.DataTable[] dt = new System.Data.DataTable[4];
                string etc_data = start < DateTime.Today.AddDays(-1) ? "server_data_total" + start.AddDays(2).ToString("yyyyMM") : "server_data_temp";
                string sql = "";
                sql += "DROP TABLE IF EXISTS `cal_data`;";
                sql += "DROP TABLE IF EXISTS `twoetc`;";
                sql += "DROP TABLE IF EXISTS `twopic`;";
                sql += String.Format("CREATE TABLE cal_data SELECT * FROM {0} WHERE trade_stamp>='{1}' AND trade_stamp<='{2}';", etc_data, start.ToString("yyyy-MM-dd"), end.ToString("yyyy-MM-dd"));
                sql += @"CREATE TABLE twoetc SELECT gantry_id,etc_id,etc_plate_num,pic_plate_num,trade_status,trade_stamp,vehicle_class,self_count FROM cal_data t1
                            WHERE etc_id in
                            (SELECT etc_id FROM cal_data
                            WHERE 
                            trade_status!='0000'
                            GROUP BY etc_id
                            HAVING
                            COUNT(DISTINCT(gantry_id)) = 2)
                            AND trade_status!='0000'
                            ORDER  BY etc_id,trade_stamp;";
                sql += @"CREATE TABLE twopic SELECT gantry_id,pic_plate_num, pic_time,vehicle_class,self_count,pic_path FROM cal_data
                            WHERE pic_plate_num in
                            (SELECT
	                            pic_plate_num 
                            FROM
	                            cal_data 
                            WHERE
                                pic_plate_num != '默A00000' 
	                            AND pic_plate_num != '' 
                            GROUP BY
	                            pic_plate_num 
                            HAVING
	                            COUNT( DISTINCT ( gantry_id ) ) =2)
                            AND type = 'C' 
                            ORDER BY pic_plate_num,trade_stamp";//第三个门架非C类数据的情况要去除，添加图片路径列
                if (!DBService.Execute(sql))
                {
                    Accordtxt("创建表单失败！");
                    return;
                }
                //两次交易失败
                sql = @"SELECT * FROM twoetc
                        WHERE etc_id in 
                        (SELECT t1.etc_id FROM twoetc t1,twoetc t2
                        WHERE t1.gantry_id='G000511001000110965' and t2.gantry_id='G000511001000110909'
                        AND t1.etc_id=t2.etc_id)
                        UNION
                        SELECT * FROM twoetc
                        WHERE etc_id in 
                        (SELECT t1.etc_id FROM twoetc t1,twoetc t2
                        WHERE t1.gantry_id='G000511001000110909' and t2.gantry_id='G003061001000120010'
                        AND t1.etc_id=t2.etc_id)
                        ORDER BY etc_id,trade_stamp";
                dt[0] = IcyDB.QueryTable(sql);
                //三次交易失败
                sql = @"SELECT gantry_id,etc_id,etc_plate_num,trade_status,trade_stamp,vehicle_class,self_count FROM cal_data
                            WHERE etc_id in
                            (SELECT
	                            etc_id 
                            FROM
	                            cal_data 
                            WHERE
	                            trade_status!='0000'
                            GROUP BY
	                            etc_id 
                            HAVING
	                            COUNT( DISTINCT ( gantry_id ) ) =3 ORDER BY etc_id)
                            ORDER BY etc_id,trade_stamp";
                dt[1] = IcyDB.QueryTable(sql);
                //两个纯图
                sql = @"SELECT * FROM twopic
                        WHERE pic_plate_num in 
                        (SELECT t1.pic_plate_num FROM twopic t1,twopic t2
                        WHERE t1.gantry_id='G000511001000110965' and t2.gantry_id='G000511001000110909'
                        AND t1.pic_plate_num=t2.pic_plate_num)
                        UNION
                        SELECT * FROM twopic
                        WHERE pic_plate_num in 
                        (SELECT t1.pic_plate_num FROM twopic t1,twopic t2
                        WHERE t1.gantry_id='G000511001000110909' and t2.gantry_id='G003061001000120010'
                        AND t1.pic_plate_num=t2.pic_plate_num)
                        ORDER BY pic_plate_num,pic_time";
                dt[2] = IcyDB.QueryTable(sql);
                //三个纯图
                sql = @"SELECT gantry_id,pic_plate_num, pic_time,vehicle_class,self_count,pic_path FROM cal_data
                        WHERE pic_plate_num in
                        (SELECT
	                        pic_plate_num 
                        FROM
	                        cal_data 
                        WHERE
	                        type = 'C' 
	                        AND pic_plate_num != '默A00000' 
	                        AND pic_plate_num != '' 
                        GROUP BY
	                        pic_plate_num 
                        HAVING
	                        COUNT( DISTINCT ( gantry_id ) ) =3)
                        AND type = 'C' 
                        ORDER BY pic_plate_num,trade_stamp";
                dt[3] = IcyDB.QueryTable(sql);
                string filename = System.Environment.CurrentDirectory + "/cal/" + start.ToString("yyyy-MM-dd") + ".xlsx";
                string[] sheet_names = { "两次交易失败", "三次交易失败", "两个纯图", "三个纯图" };
                DataTable2Excel(dt, filename, sheet_names);
            }
            Accordtxt("统计完成，请见cal文件夹！");
        }

        private void Btn_AI_Analyse_Click(object sender, EventArgs e)//ETC/AI对比分析
        {
            try
            {
                DateTime t1 = DateTime.Now;
                bool connStatus = DBService.testCon("MYSQL", "127.0.0.1", "root", "12345", "ElectTollData", 5);
                if (!connStatus)
                {
                    Accordtxt("数据库连接失败！");
                    return;
                }
                string sql = "";

                DateTime start1 = Convert.ToDateTime(dateTimePicker1.Text);
                DateTime end1 = Convert.ToDateTime(dateTimePicker2.Text);
                for (int i = 0; i < (end1 - start1).Days; i++)
                {
                    DateTime start = start1.AddDays(i);
                    DateTime end = start.AddDays(1);
                    string ai_path_table = start < DateTime.Today.AddDays(-1) ? "server_ai_path_total" + start.AddDays(2).ToString("yyyyMM") : "server_ai_path_temp";
                    string etc_path_table = start < DateTime.Today.AddDays(-1) ? "server_path_total" + start.AddDays(2).ToString("yyyyMM") : "server_path_temp";
                    string etc_data = start < DateTime.Today.AddDays(-1) ? "server_data_total" + start.AddDays(2).ToString("yyyyMM") : "server_data_temp";
                    System.Data.DataTable[] dt = new System.Data.DataTable[6];

                    sql += "DROP TABLE IF EXISTS `ai_path`;";
                    sql += "DROP TABLE IF EXISTS `etc_path`;";
                    sql += "DROP TABLE IF EXISTS `etc_data`;";
                    sql += String.Format("CREATE TABLE ai_path SELECT * FROM {0} WHERE pic_time1>='{1}' AND pic_time1<='{2}';", ai_path_table, start.ToString("yyyy-MM-dd"), end.ToString("yyyy-MM-dd"));
                    sql += String.Format("CREATE TABLE etc_path SELECT * FROM {0} WHERE time1>='{1}' AND time1<='{2}';", etc_path_table, start.ToString("yyyy-MM-dd"), end.ToString("yyyy-MM-dd"));
                    sql += String.Format("CREATE TABLE etc_data SELECT * FROM {0} WHERE trade_stamp>='{1}' AND trade_stamp<='{2}';", etc_data, start.ToString("yyyy-MM-dd"), end.ToString("yyyy-MM-dd"));

                    if (!DBService.Execute(sql))
                    {
                        Accordtxt("创建表单失败！");
                        return;
                    }
                    sql = "ALTER TABLE `ai_path` ADD INDEX etc_id ( `etc_id` );";
                    sql += "ALTER TABLE `etc_path` ADD INDEX etc_id ( `etc_id` );";
                    sql += "ALTER TABLE `etc_data` ADD INDEX pic_time ( `pic_time` );";
                    sql += "update ai_path SET path2 = '' WHERE path2 is NULL;";
                    sql += "update ai_path SET path3 = '' WHERE path3 is NULL;";
                    sql += "update ai_path SET pic_time2 = '2000-01-01' WHERE pic_time2 is NULL;";
                    sql += "update ai_path SET pic_time3 = '2000-01-01' WHERE pic_time3 is NULL;";
                    sql += "update etc_path SET path2 = '' WHERE path2 is NULL;";
                    sql += "update etc_path SET path3 = '' WHERE path3 is NULL;";
                    sql += "update etc_path SET time2 = '2000-01-01' WHERE time2 is NULL;";
                    sql += "update etc_path SET time3 = '2000-01-01' WHERE time3 is NULL;";
                    if (!DBService.Execute(sql))
                    {
                        Accordtxt("表单配置失败！");
                        return;
                    }
                    //无etc_id数据
                    sql = @"SELECT id,gantry_num,etc_plate_num,pic_plate_num,path1 as ai_path1, path2 as ai_path2,path3 as ai_path3,pic_time1,pic_time2,pic_time3 FROM ai_path WHERE etc_id ='' OR etc_id ='000000000000000000000000' ORDER BY final_time";
                    dt[0] = IcyDB.QueryTable(sql);
                    //相同路径数据
                    sql = @"
                            DROP TABLE
                            IF EXISTS `same_table`;
                            CREATE TABLE `same_table` SELECT
	                            a.etc_id,
	                            a.gantry_num,
	                            a.etc_plate_num,
	                            a.pic_plate_num as ai_pic_plate_num,
	                            a.path1 AS ai_path1,
	                            a.path2 AS ai_path2,
	                            a.path3 AS ai_path3,
	                            a.pic_time1,
	                            a.pic_time2,
	                            a.pic_time3,
	                            e.count,
	                            e.path1,
	                            e.path2,
	                            e.path3,
	                            e.time1,
	                            e.time2,
	                            e.time3,
	                            a.pic_path_id,
	                            e.id AS etc_path_id 
                            FROM
	                            ai_path a,
	                            etc_path e 
                            WHERE
	                            a.etc_id = e.etc_id 
                                AND a.path1=e.path1
                                AND a.path2=e.path2 
                                AND a.path3=e.path3
	                            AND a.pic_time1 BETWEEN DATE_ADD(e.time1,INTERVAL -5 MINUTE) AND  DATE_ADD(e.time1,INTERVAL 5 MINUTE) 
	                            AND a.pic_time2 BETWEEN DATE_ADD(e.time2,INTERVAL -5 MINUTE) AND  DATE_ADD(e.time2,INTERVAL 5 MINUTE) 
                                AND a.pic_time3 BETWEEN DATE_ADD(e.time3,INTERVAL -5 MINUTE) AND  DATE_ADD(e.time3,INTERVAL 5 MINUTE) 
                            ORDER BY
	                            a.etc_id;";
                    if (!DBService.Execute(sql))
                    {
                        Accordtxt("创建same_table异常");
                        return;
                    }
                    sql = "SELECT * FROM same_table";
                    dt[2] = IcyDB.QueryTable(sql);

                    //ai与etc不同数据
                    sql = @"DROP TABLE
                            IF EXISTS `diff_table`;
                            CREATE TABLE `diff_table` SELECT
                            a.etc_id,
                            a.gantry_num,
                            a.pic_plate_num as ai_pic_plate_num,
                            a.path1 AS ai_path1,
                            a.path2 AS ai_path2,
                            a.path3 AS ai_path3,
                            a.pic_time1,
                            a.pic_time2,
                            a.pic_time3,
                            e.count,
                            e.path1,
                            e.path2,
                            e.path3,
                            e.time1, 
                            e.time2,
                            e.time3,
                            a.pic_path_id,
                            e.id as etc_path_id
                            FROM
	                            ai_path a,
	                            etc_path e 
                           WHERE
	                       a.etc_id = e.etc_id
                           AND (!(a.pic_time1 BETWEEN DATE_ADD(e.time1,INTERVAL -5 MINUTE) AND  DATE_ADD(e.time1,INTERVAL 5 MINUTE) 
	                       AND a.pic_time2 BETWEEN DATE_ADD(e.time2,INTERVAL -5 MINUTE) AND  DATE_ADD(e.time2,INTERVAL 5 MINUTE) 
                           AND a.pic_time3 BETWEEN DATE_ADD(e.time3,INTERVAL -5 MINUTE) AND  DATE_ADD(e.time3,INTERVAL 5 MINUTE))
                            OR
                            !(a.path1=e.path1
                            AND a.path2=e.path2 
                            AND a.path3=e.path3))
                            AND a.pic_path_id NOT IN (SELECT pic_path_id FROM same_table)
                            AND e.id NOT IN (SELECT etc_path_id FROM same_table)
                            ORDER BY
	                            a.etc_id;";//路径不同，时间在5分钟内，同时pic_path_id,etc_path_id 不在相同路径表中了，此时假定的情况是
                    sql += "ALTER TABLE `diff_table` ADD id int first;";
                    sql += "ALTER TABLE `diff_table` CHANGE id id int NOT NULL AUTO_INCREMENT PRIMARY KEY; ";
                    if (!DBService.Execute(sql))
                    {
                        Accordtxt("创建diff_table异常");
                        return;
                    }
                    sql = "SELECT * FROM diff_table";
                    dt[1] = IcyDB.QueryTable(sql);

                    if (Ckb_PicCopy.Checked)
                    {
                        PicGenarate(dt[0], start.ToString("yyyy-MM-dd") + "纯图");
                        PicGenarate(dt[1], start.ToString("yyyy-MM-dd"));
                    }


                    //纯ETC数据
                    sql = @"SELECT * FROM etc_path WHERE id NOT IN (SELECT etc_path_id FROM diff_table) AND  id NOT IN (SELECT etc_path_id FROM same_table)  ";
                    dt[3] = IcyDB.QueryTable(sql);

                    //分布数据
                    sql = @"SELECT count AS 通过门架数,COUNT(*) AS ETC、AI条数 FROM etc_path GROUP BY count 
                            UNION
                            SELECT gantry_num,COUNT(*) FROM ai_path GROUP BY gantry_num";
                    dt[4] = IcyDB.QueryTable(sql);



                    sql = @"SELECT * FROM etc_data WHERE etc_id IN (SELECT etc_id FROM etc_data WHERE valid_flag>0) ORDER BY etc_id";
                    dt[5] = IcyDB.QueryTable(sql);
                    string filename = System.Environment.CurrentDirectory + "/Result/" + start.ToString("yyyy-MM-dd") + "ETCAI对比分析" + ".xlsx";
                    string[] sheet_names = { "纯图还原数据", "AI_ETC路径差异数据", "AI_ETC路径相同数据", "纯ETC数据", "总体分布", "valid_flag_over_0" };
                    if (File.Exists(filename))
                    {
                        File.Delete(filename);
                    }
                    if (checkBox2.Checked)
                    {
                        ExportCsv(dt, filename, sheet_names);
                    }
                    else
                    {
                        DataTable2Excel(dt, filename, sheet_names);
                    }

                    //string file1="log/"+start.ToString("yyyy-MM-dd")+"/AIError.log";
                    //string file2="Result/"+start.ToString("yyyy-MM-dd")+"AIError.log";
                    //File.Copy(file1, file2);
                }

                DateTime t2 = DateTime.Now;
                Accordtxt("统计完成，在【Result】文件夹下！耗时：" + (t2 - t1).TotalSeconds);
            }
            catch (System.Exception ex)
            {
                Accordtxt(ex.ToString());
            }
        }

        void PicGenarate(System.Data.DataTable dt, string start)
        {
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                for (int j = 1; j < 4; j++)
                {
                    if (dr["ai_path" + j].ToString() == "")
                    {
                        break;
                    }
                    string time = Convert.ToDateTime(dr["pic_time" + j]).ToString("yyyy-MM-ddTHH:mm:ss.fff");
                    string sql = String.Format("SELECT pic_path FROM etc_data WHERE gantry_id='{0}' AND pic_time='{1}'", dr["ai_path" + j].ToString(), time);
                    System.Data.DataTable dttemp = IcyDB.QueryTable(sql);
                    if (dttemp.Rows.Count > 0)
                    {
                        string dest = "Result/" + start + "/" + dr["id"].ToString() + "/";
                        if (!Directory.Exists(dest))//创建数据文件夹
                        {
                            Directory.CreateDirectory(dest);
                        }
                        for (int k = 0; k < dttemp.Rows.Count; k++)
                        {
                            try
                            {
                                string str = dttemp.Rows[k]["pic_path"].ToString();
                                string aim = dest + dr["ai_path" + j].ToString() + "_" + Convert.ToDateTime(time).ToString("yyyy-MM-ddTHH-mm-ss-fff") + ".jpg";
                                //Accordtxt(aim);
                                //return;
                                File.Copy(str, aim);
                            }
                            catch (System.Exception ex)
                            {
                                Log.WriteLog("PicGenarateError", "", sql + "\t" + ex.ToString());
                            }
                        }
                    }
                }
            }
        }

        private void Btn_Copy_Pic_Click(object sender, EventArgs e)//根据路径拷贝图片
        {
            DialogSelect("文件(*.txt)|*.txt|所有文件|*.*", Copy_Pic);
        }
        void Copy_Pic(OpenFileDialog dialog)
        {
            richTextBox1.Clear();
            path = dialog.FileName;
            string[] linestemp = System.IO.File.ReadAllLines(path, Encoding.UTF8);//分号屏蔽
            string savepath = "Pic/" + DateTime.Now.ToString("yyyy-MM-dd") + "/";
            if (!Directory.Exists(savepath))//创建数据文件夹
            {
                Directory.CreateDirectory(savepath);
            }
            for (int i = 0; i < linestemp.Length; i++)
            {
                File.Copy(linestemp[i], savepath + Path.GetFileName(linestemp[i]));
                Accordtxt(i);
            }
            Accordtxt("完成，请在" + savepath);
        }

        private void Btn_Get_Pic_Click(object sender, EventArgs e)//获取纯图图片
        {
            Accordtxt("start");
            bool connStatus = DBService.testCon("MYSQL", "127.0.0.1", "root", "12345", "ElectTollData", 5);
            if (!connStatus)
            {
                Accordtxt("数据库连接失败！");
                return;
            }
            string sql = "SELECT id,path1 as ai_path1, path2 as ai_path2,path3 as ai_path3,pic_time1,pic_time2,pic_time3 FROM ai_path WHERE etc_id ='' OR etc_id ='000000000000000000000000' ORDER BY final_time";
            System.Data.DataTable dt = IcyDB.QueryTable(sql);
            //Accordtxt(dt.Rows.Count);
            //DateTime date = Convert.ToDateTime(dateTimePicker1.Text);
            PicGenarate(dt, DateTime.Now.ToString("yyyy-MM-dd") + "纯图");
            Accordtxt("Over");
        }

        private void Btn_ErrorOBU_Click(object sender, EventArgs e)//获取异常OBU
        {
            bool connStatus = DBService.testCon("MYSQL", "127.0.0.1", "root", "12345", "ElectTollData", 5);
            if (!connStatus)
            {
                Accordtxt("数据库连接失败！");
                return;
            }
            DateTime start1 = Convert.ToDateTime(dateTimePicker1.Text);
            DateTime end1 = Convert.ToDateTime(dateTimePicker2.Text);
            for (int i = 0; i < (end1 - start1).Days; i++)
            {
                DateTime start = start1.AddDays(i);
                DateTime end = start.AddDays(1);

                string sql = "";
                System.Data.DataTable[] dt = new System.Data.DataTable[2];
                sql = @"SELECT
	                            etc_plate_num 
                            FROM
	                            cal_data 
                            WHERE
	                            type = 'C' 
	                            AND etc_plate_num != '默A00000' 
	                            AND etc_plate_num != '' 
                            GROUP BY
	                            etc_plate_num 
                            HAVING
	                            COUNT( DISTINCT ( gantry_id ) ) =3";
                dt[0] = IcyDB.QueryTable(sql);
                List<System.Data.DataTable> list_datatable = new List<System.Data.DataTable>();
                for (int j = 0; j < dt[0].Rows.Count; j++)
                {
                    string etc_plate_num = dt[0].Rows[j]["etc_plate_num"].ToString();
                    string search_str = "SELECT * FROM server_data_total202009 WHERE etc_plate_num='" + etc_plate_num + "'";
                    System.Data.DataTable temp_dt = IcyDB.QueryTable(search_str);
                    if (temp_dt.Rows.Count > 3)
                    {
                        bool bool_result = true;
                        for (int k = 0; k < temp_dt.Rows.Count; k++)
                        {
                            if (temp_dt.Rows[k]["type"].ToString() != "C")
                            {
                                bool_result = false;
                                break;
                            }
                        }
                        if (bool_result)
                        {
                            list_datatable.Add(temp_dt);
                        }
                    }
                }
                if (list_datatable.Count > 0)
                {
                    System.Data.DataTable newDataTable = list_datatable[0].Clone();                //创建新表 克隆以有表的架构。
                    object[] objArray = new object[newDataTable.Columns.Count];   //定义与表列数相同的对象数组 存放表的一行的值。
                    for (int m = 0; m < list_datatable.Count; m++)
                    {
                        try
                        {
                            for (int j = 0; j < list_datatable[m].Rows.Count; j++)
                            {
                                list_datatable[m].Rows[j].ItemArray.CopyTo(objArray, 0);    //将表的一行的值存放数组中。
                                objArray[0] = list_datatable[m].Rows.Count;
                                newDataTable.Rows.Add(objArray);                       //将数组的值添加到新表中。
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    dataTableToCsvT(newDataTable, "OBU异常数据.csv");
                }
            }
        }
        private void Btn_MaxTime_Click(object sender, EventArgs e)
        {
            bool connStatus = DBService.testCon("MYSQL", "127.0.0.1", "root", "12345", "ElectTollData", 5);
            if (!connStatus)
            {
                Accordtxt("数据库连接失败！");
                return;
            }
            string sql = "SELECT id,trade_stamp,time1 FROM server_path_temp WHERE count>1";
            System.Data.DataTable dt = IcyDB.QueryTable(sql);
            TimeSpan ts = new TimeSpan();
            string id = "";
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DateTime dt1 = Convert.ToDateTime(dt.Rows[i]["time1"]);
                DateTime dt2 = Convert.ToDateTime(dt.Rows[i]["trade_stamp"]);
                if (dt2 - dt1 > ts)
                {
                    ts = dt2 - dt1;
                    id = dt.Rows[i]["id"].ToString();
                }
            }
            Accordtxt("Over:" + id + "   " + ts.TotalHours);
        }

        #endregion



        private void Btn_Update_Click(object sender, EventArgs e)//一键更新
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "文件(*.*)|*.*|所有文件|*.*";
                dialog.Multiselect = true;
                dialog.ValidateNames = true;
                dialog.CheckPathExists = true;
                dialog.CheckFileExists = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = "";
                    DateTime t1 = DateTime.Now;
                    string position = "/home/data/Debug/";
                    using (var sftpClient = new SftpClient("192.168.0.100", 22, "root", "wanji@300552"))
                    {
                        sftpClient.Connect();

                        if (!sftpClient.Exists(position))
                        {
                            position = "/data/Debug/";
                        }
                        for (int i = 0; i < dialog.FileNames.Length; i++)
                        {
                            fileName = dialog.FileNames[i];
                            try
                            {
                                Stream stream1 = File.Open(fileName, FileMode.Open);
                                sftpClient.UploadFile(stream1, position + Path.GetFileName(fileName));
                                stream1.Dispose();
                            }
                            catch (System.Exception ex)
                            {
                                Accordtxt(fileName + "\n" + ex.ToString());
                            }
                        }
                        sftpClient.Disconnect();
                    }
                    string cmd = "cd " + position + ";mono ElectToll.exe";
                    using (var sshClient = new SshClient("192.168.0.100", 22, "root", "wanji@300552"))
                    {
                        sshClient.Connect();
                        var result = sshClient.RunCommand(cmd);
                        Accordtxt(result.Result);
                        sshClient.Disconnect();
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }


        private void Btn_FindRepeat_Click(object sender, EventArgs e)//WriteJson 查找重复
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                if (path == "")
                {
                    dialog.InitialDirectory = System.Environment.CurrentDirectory + "/";
                }
                else
                {
                    dialog.InitialDirectory = path;
                }
                dialog.Filter = "文件(*.log)|*.log|所有文件|*.*";
                dialog.Multiselect = true;
                dialog.ValidateNames = true;
                dialog.CheckPathExists = true;
                dialog.CheckFileExists = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    path = dialog.FileNames[0];
                    string fileName = "";
                    DateTime t1 = DateTime.Now;
                    for (int k = 0; k < dialog.FileNames.Length; k++)
                    {
                        fileName = dialog.FileNames[k];
                        string[] Lines = File.ReadAllLines(fileName);
                        for (int i = 0; i < Lines.Length; i++)
                        {
                            string tempstr = Lines[i].Substring(Lines[i].IndexOf('G'));
                            for (int j = i + 1; j < i + 30; j++)
                            {
                                if (j == Lines.Length)
                                {
                                    break;
                                }
                                if (Lines[j].Substring(Lines[j].IndexOf('G')) == tempstr)
                                {
                                    Accordtxt(tempstr);
                                    break;
                                }
                            }
                        }


                        Accordtxt("时间异常");
                        string temp = Lines[0].Substring(0, Lines[0].IndexOf("->"));
                        DateTime last_dt = DateTime.Parse(temp);
                        for (int i = 0; i < Lines.Length; i++)
                        {
                            try
                            {
                                string tempstr = Lines[i].Substring(0, Lines[i].IndexOf("->"));
                                DateTime now_dt = DateTime.Parse(tempstr);
                                if (now_dt < last_dt.AddSeconds(-1))
                                {
                                    Accordtxt("时间相差间隔：" + (now_dt - last_dt).TotalSeconds + "\n" + Lines[i]);
                                }
                                last_dt = now_dt;
                            }
                            catch (System.Exception)
                            {
                                Accordtxt(Lines[i]);
                            }

                        }
                        DateTime t2 = DateTime.Now;
                        Accordtxt((t2 - t1).TotalMilliseconds.ToString());
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }


        private void Btn_Rename_Click(object sender, EventArgs e)//批量重命名
        {
            Save(Resources.Rename, "Rename.exe");
            Process.Start("Rename.exe");
            string str = "\n图片Json取时间，选择替换页签，勾选正则表达式\n第一行输入：" + @"(\w{38})_(\d{9})_";
            str += "\n第二行输入：$2_$1_\n";
            Accordtxt(str);
        }
        private void Btn_PicAnalyse_Click(object sender, EventArgs e)//大系统数据分析
        {
            FormPicAnalyse f1 = new FormPicAnalyse();
            f1.StartPosition = FormStartPosition.CenterScreen;
            f1.Show();
        }
        private void Btn_SimuLaserHeartBeat_Click(object sender, EventArgs e)//模拟激光心跳
        {
            IcyThread.Start(ThreadSimuLaserHeartBeat);
        }
        void ThreadSimuLaserHeartBeat()
        {
            Accordtxt("模拟激光心跳测试开启");
            while (true)
            {
                string reContent = IcyHttp.WebApiPost("http://192.168.0.100:8886/LaserSynTime_" + DateTime.Now.ToString("yyyyMMddHHmmssfff"), "");//匝道
                Log.WriteLog("Laser", "HeartBeat", reContent);
                txb_send_num.Text = (int.Parse(txb_send_num.Text) + 1).ToString();
                Thread.Sleep(5000);
            }
        }
        private void Btn_PingLaser_Click(object sender, EventArgs e)//ping 激光
        {
            Save(Resources.AutoPingServ, "AutoPingServ.exe");
            Save(Resources.iplist, "iplist.json");
            Process.Start("AutoPingServ.exe");
        }
        private void Btn_485_Click(object sender, EventArgs e)//485触发软件
        {
            Form485 f1 = new Form485();
            f1.StartPosition = FormStartPosition.CenterScreen;
            f1.Show();
        }
        private void Btn_CheatAnalyse_Click(object sender, EventArgs e)//疑似作弊分析
        {
            FormCheatAnalyse f1 = new FormCheatAnalyse();
            f1.StartPosition = FormStartPosition.CenterScreen;
            f1.Show();
        }



        private void Btn_DBInfo_Click(object sender, EventArgs e)//批量插入数据库
        {
            DialogSelect("文件(*.txt;*.log)|*.txt;*.log|所有文件|*.*", DbInfo);
        }
        void DbInfo(OpenFileDialog dialog)
        {
            DateTime t1 = DateTime.Now;
            path = dialog.FileName;
            string extension = Path.GetExtension(path);
            string[] Lines = System.IO.File.ReadAllLines(path, Encoding.UTF8);
            string addstr = "(`RecordTime`, `MatchType`, `Repeat`, `UnEffect`, `VirtualPosX`, `VirtualPosY`, `RealityPosX`, `RealityPosY`, `Las5_TrigLine`, `Las5_LaneNo`, `Las5_VehId`, `Las5_LocalTime`, `Las5_DetectTime`, `Las5_CorrdX`, `Las5_CorrdY`, `Las5_SpeedVeh`, `Las5_SpeedUse`, `Las5_SpeedAvg`, `Las5_RangeY`, `Las5_RangeN`, `Las4_TrigLine`, `Las4_LaneNo`, `Las4_VehId`, `Las4_LocalTime`, `Las4_DetectTime`, `Las4_CorrdX`, `Las4_CorrdY`, `Las4_SpeedVeh`, `Las4_SpeedUse`, `Las4_SpeedAvg`, `Las4_RangeY`, `Las4_RangeN`, `Las3_TrigLine`, `Las3_LaneNo`, `Las3_VehId`, `Las3_LocalTime`, `Las3_DetectTime`, `Las3_CorrdX`, `Las3_CorrdY`, `Las3_SpeedVeh`, `Las3_SpeedUse`, `Las3_SpeedAvg`, `Las3_RangeY`, `Las3_RangeN`, `Cap1_Frm`, `Cap1_LaneNo`, `Cap1_HeadId`, `Cap1_PltId`, `Cap1_LocalTime`, `Cap1_DetectTime`, `Cap1_Plate`, `Cap1_TrigSrc`, `Cap2_Frm`, `Cap2_LaneNo`, `Cap2_HeadId`, `Cap2_PltId`, `Cap2_LocalTime`, `Cap2_DetectTime`, `Cap2_Plate`, `Cap2_TrigSrc`, `Cap3_Frm`, `Cap3_LaneNo`, `Cap3_HeadId`, `Cap3_PltId`, `Cap3_LocalTime`, `Cap3_DetectTime`, `Cap3_Plate`, `Cap3_TrigSrc`, `Rsu_LaneNo`, `Rsu_MacId`, `Rsu_Error`, `Rsu_Valid`, `Rsu_MacIdRelate`, `Rsu_ErrorRelate`, `Rsu_ValidRelate`, `Rsu_LocalTime`, `Rsu_DetectTime`, `Rsu_PlateIcc`, `Rsu_PlateObu`, `Rsu_VstX1`, `Rsu_VstY1`, `Rsu_TrdX2`, `Rsu_TrdY2`, `Rsu_TrdX3`, `Rsu_TrdY3`, `Rsu_TrdX4`, `Rsu_TrdY4`, `Rsu_TrdX5`, `Rsu_TrdY5`, `Rsu_SelfSta`, `VehJudge`, `LineLaser`, `LineVideo`, `PicTotal`, `PicPath1`, `PicPath2`, `PicPath3`, `PicPath4`, `PicPath5`, `PicPath6`)";
            string table_name = Txb_MysqlTableName.Text.Trim();//新表名
            Accordtxt(table_name + "表正在插入请稍候......");
            for (int i = 0; i < Lines.Length; i++)
            {
                if (table_name == "tolldata")
                {
                    Lines[i] = Lines[i].Substring(Lines[i].IndexOf("INSERT")).Replace("VALUES", addstr + " VALUES");
                }
                else
                {
                    Lines[i] = Lines[i].Substring(Lines[i].IndexOf("INSERT")).Replace("VALUES", addstr + " VALUES").Replace("`tolldata`", table_name);
                }
            }
            MysqlInsert(Lines, table_name);
            DateTime t2 = DateTime.Now;
            Accordtxt("总耗时：" + (t2 - t1).ToString());
        }
        void MysqlInsert(string[] Lines, string table_name)
        {
            string connectionString = "server=localhost" + ";port=3306;user id=root" + ";password=12345" + ";database=electtolldata" + ";Charset=utf8";
            MySqlConnection mycon = new MySqlConnection(connectionString);
            mycon.Open();
            MySqlCommand cmd = new MySqlCommand();
            string sql = String.Format("show tables like '{0}'", table_name);
            cmd = new MySqlCommand(sql, mycon);
            var result = cmd.ExecuteScalar();
            if (result == null)//表不存在
            {
                cmd = new MySqlCommand(Resources.dbinfo.Replace("dbinfo", table_name), mycon);
                cmd.ExecuteNonQuery();
            }
            else
            {
                string str = "数据库dbinfo表已存在，【是】继续插入,【否】清空表继续插入，【取消】退出？";
                DialogResult MsgBoxResult = MessageBox.Show(str, "警告", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
                if (MsgBoxResult == DialogResult.Cancel)
                {
                    return;
                }
                else if (MsgBoxResult == DialogResult.No)
                {
                    sql = String.Format("DELETE FROM {0}", table_name);
                    cmd = new MySqlCommand(sql, mycon);
                    cmd.ExecuteScalar();
                }
            }
            //事务提交形式插入
            cmd.Connection = mycon;
            MySqlTransaction tx = mycon.BeginTransaction();
            cmd.Transaction = tx;

            for (int n = 0; n < Lines.Length; n++)
            {
                string strsql = Lines[n].ToString();
                if (strsql.Trim().Length > 1)
                {
                    cmd.CommandText = strsql;
                    cmd.ExecuteNonQuery();
                }
                if (n > 0 && (n % 1000 == 0 || n == Lines.Length - 1))
                {
                    tx.Commit();
                    tx = mycon.BeginTransaction();
                    Accordtxt(DateTime.Now + "第" + n + "千条数据插入成功！");
                }
            }
        }


        private void Json(object sender, EventArgs e)//Json显示
        {
            DialogSelect("文件(*.json)|*.json|所有文件|*.*", JsonShow);
        }
        void JsonShow(OpenFileDialog dialog)
        {
            string body = File.ReadAllText(dialog.FileName);
            JObject obj = JObject.Parse(body);
            if (body.Contains("bizContent"))
            {
                JObject JsonObj = JObject.Parse(obj["bizContent"].ToString());
                obj["bizContent"] = JsonObj;
                Accordtxt("\n" + obj + "\n*******************************************\n*******************************************");
            }
            else
            {
                Accordtxt("\n" + obj + "\n*******************************************\n*******************************************");
            }
        }


        private void Btn_FindPlate_Click(object sender, EventArgs e)//根据车牌查找Linux的图片拷贝到当前电脑中
        {
            string file_filter = "文件(*.ini)|*.ini|所有文件|*.*";
            DialogSelect(file_filter, FindPlate);
        }
        void FindPlate(OpenFileDialog dialog)
        {
            try
            {
                string position = Tbx_FindPlateFileDate.Text; // "/home/data/Debug/Json/";
                string name = textBox3.Text;
                string password = textBox4.Text;
                string save_position = "/home/pic_plate/";
                string[] plate_lines = File.ReadAllLines(dialog.FileName, Encoding.UTF8);
                string local = Path.GetDirectoryName(dialog.FileName) + "/PLATE/";
                string ip = Txb_IP.Text.Trim();
                if (plate_lines.Length == 0)
                {
                    Accordtxt("空文件");
                    return;
                }
                string find_string = "find POSITION -name \"*PLATE*\" -exec cp {} /home/pic_plate/ \\;";
                using (var sshClient = new SshClient(ip, 22, name, password))
                {
                    using (var sftpClient = new SftpClient(ip, 22, name, password))
                    {
                        sshClient.Connect();
                        sftpClient.Connect();
                        //if (!sftpClient.Exists(position))
                        //{
                        //    position = "/data/Debug/Json/";
                        //}
                        //if (Tbx_FindPlateFileDate.Text != "")
                        //{
                        //    position += Tbx_FindPlateFileDate.Text + "/";
                        //}
                        find_string = find_string.Replace("POSITION", position);//图片搜索文件夹，兼容/home/data/Debug/Json和/data/Debug/Json

                        //先清空存储文件夹，再创建
                        if (sftpClient.Exists(save_position))
                        {
                            sshClient.RunCommand("rm -r " + save_position);
                        }
                        sftpClient.CreateDirectory(save_position);

                        for (int i = 0; i < plate_lines.Length; i++)
                        {
                            if (plate_lines[i].Trim() == "")
                            {
                                continue;
                            }
                            string cmd = find_string.Replace("PLATE", plate_lines[i].Trim());
                            var result = sshClient.RunCommand(cmd);
                            Accordtxt(cmd);
                        }
                        if (sftpClient.Exists("/home/pic_plate.zip"))
                        {
                            sftpClient.Delete("/home/pic_plate.zip");
                        }
                        sshClient.RunCommand("zip -q -r /home/pic_plate.zip /home/pic_plate/");


                        if (!Directory.Exists(local))//创建数据文件夹
                        {
                            Directory.CreateDirectory(local);
                        }
                        using (var stream = File.Open(local + DateTime.Now.ToString("MM-dd-HH-mm") + "pic_plate.zip", FileMode.Create))
                        {
                            sftpClient.DownloadFile("/home/pic_plate.zip", stream);
                        }
                        sftpClient.Disconnect();
                    }
                    sshClient.Disconnect();
                }
                Accordtxt("查找结束，图片位置为：" + local + DateTime.Now.ToString("MM-dd-HH-mm") + "pic_plate.zip");
            }
            catch (System.Exception ex)
            {
                Accordtxt(ex.ToString());
            }
        }

        private void Btn_MultiLogAnalyse_Click(object sender, EventArgs e)
        {
            FormMultiLog f1 = new FormMultiLog();
            f1.StartPosition = FormStartPosition.CenterScreen;
            f1.Show();
        }


        void GetData()
        {
            string url = textBox1.Text;
            string str = richTextBox2.Text;
            string reContent = IcyHttp.WebApiPost(url, str);
            Accordtxt(reContent);
            Log.WriteLog("Test", "Camera", reContent);
        }



        private void button8_Click(object sender, EventArgs e)
        {
            //Stream stream = new MemoryStream(Resources.WJMAIN_20210426_Brn);
            //string name = string.Join(",", System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames());
            //string password = IcyEncrypt.GetMD5_Base64("20210426wanji").Substring(0, 16);

            //UnZip(stream, "10.100.8.49", password);
        }
        void UnZip(Stream stream, string ip, string password)
        {
            string position = "/home/data/Debug/";
            string filename = "";
            using (var sftpClient = new SftpClient(ip, 22, "root", "wanji@300552"))
            {
                sftpClient.Connect();
                if (!sftpClient.Exists(position))
                {
                    position = "/data/Debug/";
                }
                try
                {
                    using (ZipInputStream zipfiles = new ZipInputStream(stream))
                    {
                        zipfiles.Password = password;
                        ZipEntry theEntry;
                        while ((theEntry = zipfiles.GetNextEntry()) != null)
                        {
                            filename = theEntry.Name;
                            int size = 2048;
                            byte[] data = new byte[2048];
                            List<byte> list_data = new List<byte>();
                            while (true)
                            {
                                size = zipfiles.Read(data, 0, data.Length);
                                if (size == data.Length)
                                {
                                    list_data.AddRange(data);
                                }
                                else if (size == 0)
                                {
                                    break;
                                }
                                else//尾部数据
                                {
                                    byte[] temp_data = new byte[size];
                                    Array.Copy(data, temp_data, size);
                                    list_data.AddRange(temp_data);
                                }
                            }
                            Stream stream1 = new MemoryStream(list_data.ToArray());
                            sftpClient.UploadFile(stream1, position + Path.GetFileName(filename));
                            stream1.Dispose();
                        }
                        zipfiles.Close();
                    }
                }
                catch (System.Exception ex)
                {
                    Accordtxt(filename + "\n" + ex.ToString());
                }
                sftpClient.Disconnect();
            }
        }
        private void Btn_CameraTest_Click(object sender, EventArgs e)
        {
            GetData();
            System.Timers.Timer t1 = new System.Timers.Timer();
            t1.Interval = 1800000;
            t1.Elapsed += delegate { GetData(); };
            t1.Enabled = true;
        }

        private void Btn_GetCpuMemory_Click(object sender, EventArgs e)
        {
            GetCpuMemory();
            System.Windows.Forms.Timer t1 = new System.Windows.Forms.Timer();
            t1.Interval = 30000;
            t1.Tick += delegate { GetCpuMemory(); };
            t1.Start();
        }
        void GetCpuMemory()
        {
            if (IcySysInfo.IsWindows)
            {
                string str = "\nCPU使用率：" + IcySysInfo.Instance.CpuUsed.ToString(".00") + "%\n"
                                 + "系统总计内存：" + IcySysInfo.Instance.MemTotal_M + "M\n"
                                 + "系统已使用内存：" + IcySysInfo.Instance.MemUsed_M + "M\n"
                                 + "系统总计硬盘：" + IcySysInfo.Instance.DiskTotal_M / 1024 + "G\n"
                                 + "系统已使用硬盘：" + IcySysInfo.Instance.DiskUsed_M / 1024 + "G";
                Accordtxt(str);
            }
        }

        private void Btn_LaserSerialTest_Click(object sender, EventArgs e)
        {
            comOpen(Tbx_Com1.Text, 115200, true);
            comOpen(Tbx_Com2.Text, 115200, true);
        }
     
        public static string byteToHexStr(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("X2")+" ";
                }
            }
            return returnStr;
        }

        public void comOpen(string portname, int baudrate, bool rtsEnable)
        {
            try
            {
                SerialPort com = new SerialPort();
                com.PortName = portname;
                com.BaudRate = baudrate;
                com.Open();
                if (com.IsOpen)
                {
                    IcyThread.Start(SerialPortRead, com);
                }
            }
            catch (System.Exception ex)
            {
                Accordtxt(ex.ToString());
            }
        }

        public void SerialPortRead(object com)
        {
            SerialPort comm1 = (SerialPort)com;
            byte[] l_RecvBuf = new byte[2048];
            byte[] l_RecvParseBuf = new byte[2048];
            int gReciveFlag = 0;
            while (true)
            {
                int recvCount = comm1.Read(l_RecvBuf, 0, 2048);
                if (recvCount > 0)
                {
                    for (int i = 0; i < recvCount; i++)
                    {
                        if (gReciveFlag <=1)
                        {
                            if (l_RecvBuf[i] == 0xFF)
                            {
                                l_RecvParseBuf[gReciveFlag++] = l_RecvBuf[i];
                            }
                        }
                        else
                        {
                            if (l_RecvBuf[i] == 0xFF) //找到帧尾
                            {
                                l_RecvParseBuf[gReciveFlag++] = l_RecvBuf[i];
                                byte[] data = new byte[gReciveFlag];
                                Array.Copy(l_RecvParseBuf, data, gReciveFlag);
                                Array.Clear(l_RecvParseBuf, 0, 2048);
                                gReciveFlag = 0;
                                Log.WriteLog("Laser" + comm1.PortName, "", byteToHexStr(data));
                            }
                            else
                            {
                                l_RecvParseBuf[gReciveFlag++] = l_RecvBuf[i];
                            }
                        }
                    }
                }
                Thread.Sleep(100);
            }
        }



        #region  模拟数据仿真
        Dictionary<int, object> send_file = new Dictionary<int, object>();
        List<FileInfo> json_list = new List<FileInfo>();
        List<string> rsu_list=new List<string>();
        public void ForeachFile1(string filePathByForeach)
        {
            DirectoryInfo theFolder = new DirectoryInfo(filePathByForeach);
            DirectoryInfo[] dirInfo = theFolder.GetDirectories();//获取所在目录的文件夹
            FileInfo[] file = theFolder.GetFiles();//获取所在目录的文件
            string fileName = "";
            foreach (FileInfo fileItem in file) //遍历文件
            {
                try
                {
                    if (!fileItem.Name.StartsWith("Rsu"))
                    {
                        json_list.Add(fileItem);
                    }
                    else
                    {
                        rsu_list = File.ReadAllLines(fileItem.FullName).Where(o => o.Contains("ff")).ToList(); 
                    }
                }
                catch (System.Exception ex)
                {
                    Log.WriteLog("Error", "ForeachFile1", fileName + "\n" + ex.ToString());
                }
            }
            //遍历文件夹
            foreach (DirectoryInfo NextFolder in dirInfo)
            {
                ForeachFile1(NextFolder.FullName);
            }
        }
        

        private void Btn_SimulationIni_Click(object sender, EventArgs e)
        {
            try
            {
                if (IcyTCPserver.Init(9526))
                {
                    Accordtxt("tcp server 开启成功，端口号:9526");
                }

                string filepath = "Json/Simulation/";
                var dir = new DirectoryInfo(filepath);
                if (!dir.Exists)
                {
                    dir.Create();
                    Accordtxt(filepath + " 目录下无发送文件");
                    return;
                }
                json_list.Clear();
                send_file.Clear();
                ForeachFile1(filepath);
                Dictionary<DateTime, FileInfo> dic = new Dictionary<DateTime, FileInfo>();
                var file_list = json_list.OrderBy(o => o.Name).ToList();
                DateTime time_start = DateTime.ParseExact(file_list.FirstOrDefault().Name.Substring(0, 23), "yyyy-MM-ddTHH_mm_ss_fff", System.Globalization.CultureInfo.CurrentCulture);
                DateTime time_end = DateTime.ParseExact(file_list.LastOrDefault().Name.Substring(0, 23), "yyyy-MM-ddTHH_mm_ss_fff", System.Globalization.CultureInfo.CurrentCulture);
                dateTimePicker_start.Value = time_start;
                dateTimePicker_end.Value = time_end;
            }
            catch (System.Exception ex)
            {
                Accordtxt(ex.ToString());
            }
        }





        List<Thread> list_thread = new List<Thread>();
        private void Btn_HttpSendTest_Click(object sender, EventArgs e)
        {
            rsu_list = rsu_list.Where(o => (DateTime.Parse(o.Substring(0, 23)) >= dateTimePicker_start.Value) && (DateTime.Parse(o.Substring(0, 23)) <= dateTimePicker_end.Value)).ToList();//Rsu中抽取对应时间的数据
            var file_list = json_list.Where(o => (String.Compare(o.Name.Substring(0, 23), dateTimePicker_start.Value.ToString("yyyy-MM-ddTHH_mm_ss_fff")) >= 0 && String.Compare(o.Name.Substring(0, 23), dateTimePicker_end.Value.ToString("yyyy-MM-ddTHH_mm_ss_fff")) <= 0)).OrderBy(o => o.Name).ToList();
            for (int i = 0; i < file_list.Count; i++)
            {
                DateTime time = DateTime.ParseExact(file_list[i].Name.Substring(0, 23), "yyyy-MM-ddTHH_mm_ss_fff", System.Globalization.CultureInfo.CurrentCulture);
                int index = (int)(time - dateTimePicker_start.Value).TotalMilliseconds;
                while (send_file.ContainsKey(index))//key错开1ms累加
                {
                    index++;
                }
                send_file.Add(index, file_list[i]);
            }
            for (int i = 0; i < rsu_list.Count; i++)
            {
                DateTime time = DateTime.Parse(rsu_list[i].Substring(0, 23));
                int index = (int)(time - dateTimePicker_start.Value).TotalMilliseconds;
                while (send_file.ContainsKey(index))//key错开1ms累加
                {
                    index++;
                }
                string data = rsu_list[i].Substring(rsu_list[i].IndexOf("ff"));
                send_file.Add(index, data);
            }
            send_file = send_file.OrderBy(o => o.Key).ToDictionary(o => o.Key, p => p.Value);

            list_thread.Add(IcyThread.Start(Sendfor));
            list_thread.Add(IcyThread.Start(ThreadRSU));
            list_thread.Add(IcyThread.Start(ThreadPLaser));
            list_thread.Add(IcyThread.Start(ThreadCLaser));
            list_thread.Add(IcyThread.Start(ThreadVehicle));
        }

        //激光数据和车型识别数据按照队列顺序发送
        private static readonly ConcurrentQueue<int> PLaserQueue = new ConcurrentQueue<int>();
        private static readonly ConcurrentQueue<int> CLaserQueue = new ConcurrentQueue<int>();
        private static readonly ConcurrentQueue<int> VehicleQueue = new ConcurrentQueue<int>();
        private static readonly ConcurrentQueue<string> RSUQueue = new ConcurrentQueue<string>();
        private void ThreadPLaser()
        {
            while (true)
            {
                int msg;
                while (PLaserQueue.Count > 0 && PLaserQueue.TryDequeue(out msg))
                {
                    Send1(msg);
                }
                Thread.Sleep(10);
            }
        }
        private void ThreadCLaser()
        {
            while (true)
            {
                int msg;
                while (CLaserQueue.Count > 0 && CLaserQueue.TryDequeue(out msg))
                {
                    Send1(msg);
                }
                Thread.Sleep(10);
            }
        }
        private void ThreadVehicle()
        {
            while (true)
            {
                int msg;
                while (VehicleQueue.Count > 0 && VehicleQueue.TryDequeue(out msg))
                {
                    Send1(msg);
                }
                Thread.Sleep(10);
            }
        }

        private void ThreadRSU()
        {
            while (true)
            {
                string msg;
                while (RSUQueue.Count > 0 && RSUQueue.TryDequeue(out msg))
                {
                    byte[] send_byte = StrToHexByte(msg);
                    IcyTCPserver.SendAll(send_byte);
                    //Log.WriteLog("RetryPost", "Send", msg);
                }
                Thread.Sleep(1);
            }
        }

        void Sendfor()
        {
            task_list.Clear();
            Accordtxt("开始发送");
            int sendindex = 1;
            while (checkBox_LoopSend.Checked || sendindex <= 1)
            {
                for (int i = 0; i < send_file.Count; i++)
                {
                    string str = send_file.ElementAt(i).Value.ToString();
                    if (str.StartsWith("ff"))
                    {
                        if (IcyTCPserver.clients.Count > 0)
                        {
                            RSUQueue.Enqueue(str);
                        }
                        else
                        {
                            Accordtxt("Tcp 未连接");
                        }
                    }
                    else if (((FileInfo)send_file.ElementAt(i).Value).FullName.Contains("PLaser"))
                    {
                        PLaserQueue.Enqueue(send_file.ElementAt(i).Key);
                    }
                    else if (((FileInfo)send_file.ElementAt(i).Value).FullName.Contains("CLaser"))
                    {
                        CLaserQueue.Enqueue(send_file.ElementAt(i).Key);
                    }
                    else if (((FileInfo)send_file.ElementAt(i).Value).FullName.Contains("Vehicle"))
                    {
                        VehicleQueue.Enqueue(send_file.ElementAt(i).Key);
                    }
                    else
                    {
                        Task.Factory.StartNew(Send1, send_file.ElementAt(i).Key);
                    }
                    if (i == send_file.Count - 1)
                    {
                        break;
                    }
                    int interval = send_file.ElementAt(i + 1).Key - send_file.ElementAt(i).Key;
                    Thread.Sleep(interval);//睡1ms有偏差，当多次累积sleep(1) 会形成较大偏差
                }
                Accordtxt("循环发送：" + sendindex + " 次");
                sendindex++;
            }
            Task.WaitAll(task_list.ToArray());
            Accordtxt("发送成功");
        }
        /// <summary>
        /// 高精度延时,窗口程序不卡死延时
        /// </summary>
        /// <param name="time">1000微秒 = 1毫秒 ； 1000毫秒 = 1秒</param>
        /// <param name="type">可空:毫秒  0：毫秒  1：微秒  2：秒  3：分  4：小时  5：天</param>
        public static void SuperSleep(int time, int type = 0)
        {
            if (time < 1)
            {
                return;
            }

            int hTimer = 0;
            long Interval = 0;
            int i = 0;

            int INFINITE = -1;
            int QS_ALLINPUT = 255;
            int WAIT_OBJECT_0 = 0;

            if (type == 1)
            {
                Interval = -10 * time;
                hTimer = CreateWaitableTimer(0, true, "WaitableTimer");
                SetWaitableTimer(hTimer, ref Interval, 0, 0, 0, false);

                while (MsgWaitForMultipleObjects(1, ref hTimer, false, INFINITE, QS_ALLINPUT) != WAIT_OBJECT_0)
                {
                    System.Windows.Forms.Application.DoEvents();
                }

                CloseHandle(hTimer);
                return;
            }
            if (type == 0)
            {
                type = 1;
            }
            if (type == 2)
            {
                type = 1000;
            }
            if (type == 3)
            {
                type = 1000 * 60;
            }
            if (type == 4)
            {
                type = 1000 * 60 * 60;
            }
            if (type == 5)
            {
                type = 1000 * 60 * 60 * 24;
            }

            Interval = -10 * time * 1000 * type;
            hTimer = CreateWaitableTimer(0, true, "WaitableTimer");
            SetWaitableTimer(hTimer, ref Interval, 0, 0, 0, false);
            while (MsgWaitForMultipleObjects(1, ref hTimer, false, INFINITE, QS_ALLINPUT) != WAIT_OBJECT_0)
            {
                System.Windows.Forms.Application.DoEvents();
            }
            CloseHandle(hTimer);
        }


        /// <summary>
        /// 创建或打开一个可等待的计时器对象
        /// </summary>
        /// <param name="lpTimerAttributes"></param>
        /// <param name="bManualReset"></param>
        /// <param name="lpTimerName"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        private static extern int CreateWaitableTimer(int lpTimerAttributes, bool bManualReset, string lpTimerName);

        /// <summary>
        /// 激活指定的等待计时器
        /// </summary>
        /// <param name="hTimer"></param>
        /// <param name="ft"></param>
        /// <param name="lPeriod"></param>
        /// <param name="pfnCompletionRoutine"></param>
        /// <param name="pArgToCompletionRoutine"></param>
        /// <param name="fResume"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        static extern bool SetWaitableTimer(int hTimer, [In] ref long pDueTime, int lPeriod, int pfnCompletionRoutine, int pArgToCompletionRoutine, bool fResume);

        /// <summary>
        /// 等待直到一个或所有指定对象处于信号状态或超时间隔过去
        /// </summary>
        /// <param name="nCount"></param>
        /// <param name="pHandles"></param>
        /// <param name="fWaitAll"></param>
        /// <param name="dwMilliseconds"></param>
        /// <param name="dwWakeMask"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        private static extern int MsgWaitForMultipleObjects(int nCount, ref int pHandles, bool fWaitAll, int dwMilliseconds, int dwWakeMask);

        /// <summary>
        /// 关闭打开的对象句柄。
        /// </summary>
        /// <param name="hObject"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        private static extern int CloseHandle(int hObject);
        void Send1(object obj)
        {


            int index = (int)obj;
            string str = send_file[index].ToString();
            try
            {
                var file = (FileInfo)send_file[index];
                string name = file.Name.Substring(24);
                string url = "";
                if (file.FullName.Contains("Camera"))
                {
                    url = "http://" + Txb_SimulationIP.Text + ":2230/" + name;
                }
                else if (file.FullName.Contains("PLaser"))
                {
                    url = "http://" + Txb_SimulationIP.Text + ":8886/" + name;
                }
                else if (file.FullName.Contains("CLaser"))
                {
                    url = "http://" + Txb_SimulationIP.Text + ":8881/" + name;
                }
                else if (file.FullName.Contains("Vehicle"))
                {
                    url = "http://" + Txb_VehicleRec.Text + ":8880/" + name;
                }
                else
                {
                    Accordtxt(name+"文件为非法文件!"); 
                    return;
                }

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json;charset=UTF-8";
                request.Timeout = 3000;
                byte[] bt = File.ReadAllBytes(file.FullName);
                request.ContentLength = bt.Length;
                request.KeepAlive = false;//ly
                Stream reqStream = request.GetRequestStream();
                reqStream.Write(bt, 0, bt.Length);
                reqStream.Close();
                string result = "";
                using (StreamReader sr = new StreamReader(request.GetResponse().GetResponseStream()))
                {
                    result = sr.ReadToEnd();
                }
                if (!string.IsNullOrEmpty(result) && !result.StartsWith("Error"))
                {
                    Log.WriteLog("RetryPost", "Send", file.Name );
                }
                else
                {
                    Log.WriteLog("RetryPostError", "SendError", file.Name + "/n" + result);
                }
            }
            catch (System.Exception ex)
            {
                Log.WriteLog("RetryPostError", "SendError", str + "\n" + ex.ToString());
            }
        }

        private static byte[] StrToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }
        #endregion


        private void Form1_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                if (Environment.MachineName == "DESKTOP-J3APA45" || Environment.MachineName == "WJ-10400556")
                {
                    string url = "http://qt.gtimg.cn/q=sz000651,sz002027,sz002304,sh600519";

                    string reContent = HttpGet(url, "");

                    string[] strarr = reContent.Split(';');
                    string str = "\n";
                    int[] nums = { 34000, 131300, 6300,100 };
                    double det = -10000 * 132;


                    int cash = 0;
                    double sum = 0;
                    for (int i = 0; i < nums.Length; i++)
                    {
                        if (strarr[i] == "")
                        {
                            continue;
                        }
                        strarr[i] = strarr[i].Split('~')[3];
                        str += string.Format("{0,-6}", double.Parse(strarr[i]).ToString("0.00")) + "\n";
                        sum += double.Parse(strarr[i]) * nums[i];
                    }
                    //for (int i = 0; i < nums.Length; i++)
                    //{
                    //    str += string.Format("{0,-4}", (double.Parse(strarr[i]) * nums[i] / sum).ToString("P2")) + "\n";
                    //}



                    int net = (int)((sum + cash)  + det);

                    str += "NET: " + net + "\n";
                    str += "ALL: " + (sum + cash) + "\n";

                    bool connStatus = DBService.testCon("MYSQL", "127.0.0.1", "root", "12345", "ElectTollData", 5);
                    if (!connStatus)
                    {
                        RunCmd("net start mysql");
                        Accordtxt("数据库连接失败！");
                        return;
                    }
                    string sql = string.Format("INSERT INTO record (date,net,det,sum,gree,fz,yh,mt,num_gree,num_fz,num_yh,num_mt) VALUES ('{0}',{1},{2},{3},'{4}','{5}','{6}','{7}',{8},{9},{10},{11}) ON DUPLICATE KEY UPDATE net= values(net),sum= values(sum),det= values(det),gree= values(gree),fz= values(fz),yh= values(yh),mt= values(mt),num_gree= values(num_gree),num_fz= values(num_fz),num_yh= values(num_yh),num_mt= values(num_mt)", DateTime.Now.ToString("yyyy-MM-dd"), net, det, sum + cash, strarr[0], strarr[1], strarr[2], strarr[3], nums[0],nums[1],nums[2], nums[3]);

                    //string sql = string.Format("REPLACE INTO record (date,net,det,sum) VALUES ('{0}',{1},{2},{3})", DateTime.Now.ToString("yyyy-MM-dd"), sum + cash + det, det, sum + cash);
                    if (!IcyDB.Execute(sql))
                    {
                        Accordtxt("Sql Error!\n"+sql);
                    }
                    Accordtxt(str);
                }
            }
            catch (System.Exception ex)
            {
                Accordtxt(ex.Message);
            }
        }

        public static string RunCmd(string cmd)
        {
            //string strInput = Console.ReadLine();
            Process p = new Process();
            //设置要启动的应用程序
            p.StartInfo.FileName = "cmd.exe";
            //是否使用操作系统shell启动
            p.StartInfo.UseShellExecute = false;
            // 接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardInput = true;
            //输出信息
            p.StartInfo.RedirectStandardOutput = true;
            // 输出错误
            p.StartInfo.RedirectStandardError = true;
            //不显示程序窗口
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            //启动程序
            p.Start();

            //向cmd窗口发送输入信息
            p.StandardInput.WriteLine(cmd + "&exit");

            p.StandardInput.AutoFlush = true;

            //获取输出信息
            string strOuput = p.StandardOutput.ReadToEnd();
            //等待程序执行完退出进程
            p.WaitForExit();
            p.Close();
            return strOuput;
            //Console.WriteLine(strOuput);
        }
        public string HttpGet(string Url, string postDataStr)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url + (postDataStr == "" ? "" : "?") + postDataStr);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString;
        }
        void UpdateData()
        {
            bool connStatus = DBService.testCon("MYSQL", "127.0.0.1", "root", "12345", "ElectTollData", 5);
            if (!connStatus)
            {
                Accordtxt("数据库连接失败！");
                return;
            }
            string sql = string.Format("UPDATE record SET det = det +1 WHERE id=2724");
            //string sql = string.Format("REPLACE INTO record (date,net,det,sum) VALUES ('{0}',{1},{2},{3})", DateTime.Now.ToString("yyyy-MM-dd"), sum + cash + det, det, sum + cash);
            if (!IcyDB.Execute(sql))
            {
                Accordtxt("Sql Error!");
            }
        }
        public static bool GetPicThumbnail(string sFile, string outPath, int flag)
        {
            System.Drawing.Image iSource = System.Drawing.Image.FromFile(sFile);
            ImageFormat tFormat = iSource.RawFormat;

            //以下代码为保存图片时，设置压缩质量  
            EncoderParameters ep = new EncoderParameters();
            long[] qy = new long[1];
            qy[0] = flag;//设置压缩的比例1-100  
            EncoderParameter eParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, qy);
            ep.Param[0] = eParam;
            try
            {
                ImageCodecInfo[] arrayICI = ImageCodecInfo.GetImageEncoders();
                ImageCodecInfo jpegICIinfo = null;
                for (int x = 0; x < arrayICI.Length; x++)
                {
                    if (arrayICI[x].FormatDescription.Equals("JPEG"))
                    {
                        jpegICIinfo = arrayICI[x];
                        break;
                    }
                }
                if (jpegICIinfo != null)
                {
                    iSource.Save(outPath, jpegICIinfo, ep);//dFile是压缩后的新路径  
                }
                else
                {
                    iSource.Save(outPath, tFormat);
                }
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                iSource.Dispose();
                iSource.Dispose();
            }
        }

        public static bool ExecuteMySql(string sql, MySqlConnection conn)
        {
            try
            {
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.CommandTimeout = 200;
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Log.write(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name,
                   System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + sql);
                return false;
            }
        }



        void FrameQueue(OpenFileDialog dialog)
        {
            string[] lines = File.ReadAllLines(dialog.FileName);
            List<int[]> frame_list=new List<int[]>();
            string test=lines[0].Substring(7);
            DateTime start =DateTime.ParseExact(test, "yyyy-M-dd-HH-mm-ss-fff", System.Globalization.CultureInfo.CurrentCulture);
            int lane_no=2;
            List<string> data = lines.Where(o => o.Contains("车道: " + lane_no)).ToList();
            for (int i = 0; i < data.Count; i++)
            {
                try
                {
                    int index_tail = data[i].IndexOf("收尾时间");
                    int index_tail_end = data[i].IndexOf(", 车型");
                    int index_frame_num = data[i].IndexOf("帧数");
                    int index_frame_num_end = data[i].IndexOf(", 触发命令");
                    string end_time = data[i].Substring(index_tail + 6, index_tail_end - index_tail - 6);
                    end_time = end_time.Substring(0, 9) + " " + end_time.Substring(10, end_time.LastIndexOf("-")-10).Replace("-",":")+"."+ end_time.Substring(end_time.LastIndexOf("-")+1);
                    int frame_num = Convert.ToInt32(data[i].Substring(index_frame_num + 3, index_frame_num_end - index_frame_num - 3));
                    DateTime end_ = Convert.ToDateTime(end_time);
                    int end = (int)(end_ - start).TotalMilliseconds / 40;
                    if (end-frame_num-2<0)
                    {
                        frame_list.Add(new int[] { 0, end + 2 });
                    }
                    else
                    {
                        frame_list.Add(new int[] { end - frame_num - 2, end + 2 });
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }


        public string PostJsonData()
        {
            string str = "";
            try
            {
                using (var client = new HttpClient())
                using (var content = new MultipartFormDataContent())
                {
                    //
                    client.BaseAddress = new Uri("http://10.92.129.38:8091");
                    var fileContent1 = new ByteArrayContent(File.ReadAllBytes(@"/home/data/ER_BVIU_REQ_G00051100200110_20170524182356001.bin"));
                    //fileContent1.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    //{
                    //    FileName = "1.json"
                    //};
                    content.Add(fileContent1, "binFile");
                    content.Add(new StringContent("ER_BVIU_REQ_G00051100200110_20170524182356001@_@json", Encoding.UTF8), "filename");
                    content.Add(new StringContent("MD5", Encoding.UTF8), "signType");
                    content.Add(new StringContent("NONE", Encoding.UTF8), "encryptType");
                    content.Add(new StringContent("1.0", Encoding.UTF8), "version");
                    content.Add(new StringContent("531A5AB51DA66986880882A794F4D777", Encoding.UTF8), "sign");

                    client.DefaultRequestHeaders.Add("binfile-gzip", "true");
                    client.DefaultRequestHeaders.Add("binfile-auth", "myTicket");
                    
                    var result = client.PostAsync("/elecfence/bin/", content).Result;
                    
                    //client.BaseAddress = new Uri("http://localhost:8989/");
                    //var fileContent1 = new ByteArrayContent(File.ReadAllBytes(@"D:\1.json"));
                    //fileContent1.Headers.ContentDisposition = new ContentDispositionHeaderValue("form")
                    //{
                    //    FileName = "1.json"
                    //};
                    //var dataContent = new ByteArrayContent(Encoding.UTF8.GetBytes("XXX_XXX_REQ_yyyyMMddHHmmssSSS.json"));
                    ////dataContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form")
                    ////{
                    ////    FileName = "type"
                    ////};
                    //content.Add(fileContent1);
                    //content.Add(dataContent);
                    //client.DefaultRequestHeaders.Add("binfile-gzip","true");
                    //client.DefaultRequestHeaders.Add("binfile-auth", "myTicket");
                    //var result = client.PostAsync("WapAPIExp/", content).Result;
                    Accordtxt(result+result.Content.ToString());
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            return str;
        }

        void TestTh()
        {
            while (true)
            {
                //Thread.Sleep(100);
            }
        }
//        string ImgClass(Mat image)
//        {
//    int classNum = 10;
//    int lowH, highH, lowS, highS, lowV, highV;
//    List<double> ptRate = new List<double>();
//    string outClass;
 
//    for (int ic = 0; ic < classNum; ic++)
//    {
//        switch (ic)
//        {
//        case 0://blue
//            lowH = 100;
//            highH = 124;
//            lowS = 63;
//            highS = 255;
//            lowV = 76;
//            highV = 220;
//            break;
//        case 1://orange
//            lowH = 11;
//            highH = 25;
//            lowS = 43;
//            highS = 255;
//            lowV = 46;
//            highV = 255;
//            break;
//        case 2://yellow
//            lowH = 22;
//            highH = 37;
//            lowS = 43;
//            highS = 255;
//            lowV = 46;
//            highV = 255;
//            break;
//        case 3:
//            lowH = 35;
//            highH = 77;
//            lowS = 43;
//            highS = 255;
//            lowV = 46;
//            highV = 255;
//            break;
//        case 4:
//            lowH = 125;
//            highH = 155;
//            lowS = 43;
//            highS = 255;
//            lowV = 46;
//            highV = 255;
//            break;
//        case 5:
//            lowH = 0;
//            highH = 10;
//            lowS = 43;
//            highS = 255;
//            lowV = 46;
//            highV = 255;
//            break;
//        case 6://white
//            lowH = 0;
//            highH = 180;
//            lowS = 0;
//            highS = 25;
//            lowV = 225;
//            highV = 255;
//            break;
//        case 7://grey
//            lowH = 0;
//            highH = 180;
//            lowS = 28;
//            highS = 40;
//            lowV = 30;
//            highV = 221;
//            break;
//        case 8://black
//            lowH = 0;
//            highH = 180;
//            lowS = 0;
//            highS = 255;
//            lowV = 0;
//            highV = 30;
//            break;
//        case 9://red
//            lowH = 156;
//            highH = 180;
//            lowS = 43;
//            highS = 255;
//            lowV = 46;
//            highV = 255;
//            break;
//        }

//        Mat imgHSV = new Mat(); ;
//        Cv2.CvtColor(image, imgHSV, ColorConversionCodes.BGR2HSV);
//        List<Mat> hsvSplit;
 
//        Cv2.Split(imgHSV, out hsvSplit);
//        Cv2.EqualizeHist(hsvSplit[2], hsvSplit[2]);
//        Cv2.Merge(hsvSplit, imgHSV);
 
//        Mat imgThresholded;
//        Cv2.InRange(imgHSV, Scalar(lowH, lowS, lowV), Scalar(highH, highS, highV), imgThresholded);
 
//        int nonZeroNum = 0;
//        List<Mat> channelsImg;
//        Cv2.Split(imgThresholded, out channelsImg);
//        Mat imgResult = channelsImg[0];
//        for (int ia = 0; ia < imgResult.Rows; ia++)
//            for (int ib = 0; ib < imgResult.Cols; ib++)
//                if (imgResult(ia, ib) != 0)
//                    nonZeroNum++;
 
//        double rateCac = (double)nonZeroNum / (double)(imgResult.Rows*imgResult.Cols);
//        ptRate[ic] = rateCac;
//    }
 
//    double curRate = 0.0;
//    int classN;
//    for (int id = 0; id < ptRate.size(); id++)
//    {
//        if (ptRate[id] > curRate)
//        {
//            curRate = ptRate[id];
//            classN = id;
//        }
//    }
	
//    switch (classN){
//    case 0:
//        outClass = "blue";
//        break;
//    case 1:
//        outClass = "orange";
//        break;
//    case 2:
//        outClass = "yellow";
//        break;
//    case 3:
//        outClass = "green";
//        break;
//    case 4:
//        outClass = "violet";
//        break;
//    case 5:
//        outClass = "red";
//        break;
//    case 6:
//        outClass = "white";
//        break;
//    case 7:
//        outClass = "grey";
//        break;
//    case 8:
//        outClass = "black";
//        break;
//    case 9:
//        outClass = "red";
//    }
 
//    return outClass;
//}
        private void Btn_Test_Click(object sender, EventArgs e)
        {
            try
            {
                //OpenFileDialog dialog = new OpenFileDialog();
                //if (path == "")
                //{
                //    dialog.InitialDirectory = System.Environment.CurrentDirectory + "/";
                //}
                //else
                //{
                //    dialog.InitialDirectory = path;
                //}
                //dialog.Filter = "文件(*.md)|*.md|所有文件|*.*";
                //dialog.ValidateNames = true;
                //dialog.CheckPathExists = true;
                //dialog.CheckFileExists = true;
                //dialog.Multiselect = true;
                //if (dialog.ShowDialog() == DialogResult.OK)
                //{
                //    path = dialog.FileNames[0];
                //    string fileName = "";
                //    task_list.Clear();
                //    DateTime t1 = DateTime.Now;
                //    for (int i = 0; i < dialog.FileNames.Length; i++)
                //    {
                //        fileName = dialog.FileNames[i];
                //        try
                //        {
                //            var lines = File.ReadAllLines(fileName);
                //            for (int j = 0; j < lines.Length; j++)
                //            {
                //                lines[j] = textBox2.Text+lines[j].Substring(1);
                //            }
                //            string str = File.ReadAllText(fileName).Substring(1);
                //            File.WriteAllLines(fileName.Replace(".md",".txt"), lines);
                //            File.Delete(fileName);
                //            //if (fileName.Contains("TRC_BVIPU_REQ") || fileName.Contains("PROOFPICTURE"))
                //            //{
                //            //    if (checkBox1.Checked)
                //            //    {
                //            //        string str = Path.GetFileName(fileName).Split('_')[2];
                //            //        if (Convert.ToInt32(str) < 256)
                //            //        {
                //            //            continue;
                //            //        }
                //            //    }
                //            //    task_list.Add(Task.Factory.StartNew(PicCatch, fileName));
                //            //    //ThreadPool.QueueUserWorkItem(PicCatch, fileName);
                //            //}
                //        }
                //        catch (System.Exception ex)
                //        {
                //            Accordtxt(fileName + "\n" + ex.ToString());
                //        }
                //    }
                //    //Task.WaitAll(task_list.ToArray());
                //    DateTime t2 = DateTime.Now;
                //    Accordtxt((t2 - t1).TotalMilliseconds.ToString() + "ms");
                //    Accordtxt(task_list.Count + " 个Json提取完成，请在照片目录下查看！");
                //}
                string filePath1 = @"C:\Users\xiaowan\documents\WeChat Files\ye035829\FileStorage\File\2024-07\Form1.txt";

                //string filePath = @"C:\Users\xiaowan\documents\WeChat Files\ye035829\FileStorage\File\2024-07\Form11.txt";
                //File.Copy(filePath1,filePath,true);
                //string notepadPlusPlusPath = @"D:\Program Files\Notepad++\notepad++.exe"; // Notepad++的安装路径
                ////string filePath = @"C:\path\to\your\file.txt"; // 要打开的文件路径

                //ProcessStartInfo startInfo = new ProcessStartInfo
                //{
                //    FileName = notepadPlusPlusPath,
                //    Arguments = "\"" + filePath + "\"",
                //    UseShellExecute = false,
                //    RedirectStandardOutput = true,
                //    CreateNoWindow = true
                //};

                //using (Process process = Process.Start(startInfo))
                //{
                //    using (StreamReader reader = process.StandardOutput)
                //    {
                //        string result = reader.ReadToEnd(); // 这里的result将包含文件的内容，但请注意，这不是直接从文件系统读取的内容
                //        Console.WriteLine(result);
                //        Accordtxt(result);
                //    }
                //}
                var lines = File.ReadAllText(filePath1);
                Accordtxt(lines);

            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

         
           
            //string[] str =File.ReadAllLines("1.onnxLabels");
            //string str1 = File.ReadAllText("1.txt",Encoding.UTF8);
            ////for (int i = 0; i < str.Length; i++)
            ////{
            ////    str1 += "\"" + str[i] + "\",";
            ////}
            //Accordtxt(str1);
            //Mat src = Cv2.ImRead("./1.jpg");
            //src = src / 127.5 - 1;
            ////src = src.Normalize(1, 0, NormTypes.MinMax, -1, null);
            //Cv2.ImShow("图像数据归一化", src);
            //Cv2.WaitKey(0);
            //Task t = new Task(() =>
            //{
            //    Console.WriteLine($"{DateTime.Now}  任务开始工作...");
            //    // 模拟工作过程
            //    Thread.Sleep(5000);
            //});
            //t.Start();
            //t.ContinueWith((task) =>
            //{
            //    Console.WriteLine($"{DateTime.Now}  任务完成，完成时的状态：");
            //    Console.WriteLine("IsCanceled={0}\tIsCompleted={1}\tIsFaulted={2}", task.IsCanceled, task.IsCompleted, task.IsFaulted);
            //});


            //var pic=File.ReadAllBytes("D:\\test\\pic\\back_ground_img_QJ5JG 2022_04_27_16_36_56_963.bmp");
            //File.WriteAllBytes("D:\\test\\pic\\back_ground_img_QJ5JG 2022_04_27_16_36_56_963.jpg",pic);


            //for (int i = 0; i < testDic.Count; i++)
            //{
            //    Accordtxt(testDic.ElementAt(i).Key + " " + testDic.ElementAt(i).Value);
            //}
            //string msg;
            //while (testDic.Count > 0 && testDic.TryRemove(out msg))
            //{
            //    Accordtxt(msg);
            //}
            //for (int i=0;i<7;i++)
            //{
            //    IcyThread.Start(TestTh);
            //}
         
            
            //PostJsonData();
            //string file_filter = "文件(*.log)|*.log|所有文件|*.*";
            // var website = "www.test.com";
            //var port = 8080;
            //var url = $"http://{website}:{port}/index.html";
            //Console.WriteLine(url);


            //DialogSelect(file_filter, FrameQueue);
            //Accordtxt(reContent);
            // int[] vehicle_num = Array.ConvertAll<object, int>(veh.ToList().ToArray(), int.Parse);

            //string sql = "select ID,ExRecordType,ExUnlawfulMold,LaneNo,DetectTime,FinalTradeResult,VehSpeed,CameraPlate,EtcPlate,CameraPlateColor,EtcPlateColor,ExCLaserVehClass,EtcVehClass,EtcObuVehClass,EtcObuVehAxles,EtcMac,EtcContractSerial,REtcCount,REtcMac,REtcContractSerial from tolldata202112 LIMIT 1";
            //Accordtxt(IcyJson.ToJsonStr(IcyDB.QueryTableMySql(sql)));
            //test(new string[0]);
            //bool connStatus = DBService.testCon("MYSQL", "127.0.0.1", "root", "12345", "ElectTollData", 5);
            //if (!connStatus)
            //{
            //    Accordtxt("数据库连接失败！");
            //    return;
            //}
            //System.Data.DataTable[] dt = new System.Data.DataTable[1];
            //dt[0] = IcyDB.QueryTable("select * from user");
            //string filename = System.Environment.CurrentDirectory  + "test.xlsx";
            //string[] sheet_names = { "test"};
            //DataTable2Excel(dt, filename, sheet_names);
            //Accordtxt("统计完成，请见cal文件夹！");
            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            //string[] list = File.ReadAllLines("Log2021-12-01.txt").Where(o => o.Contains("Unknown column 'Length' in 'field lis")).Select(o => o.Replace("日志内容：", "") + ";").ToArray();//
            //Accordtxt(sw.ElapsedMilliseconds);
            //sw.Restart();
            //string[] list1 = File.ReadAllLines("Log2021-12-01.txt").ToList().FindAll(o => o.Contains("Unknown column 'Length' in 'field lis")).Select(o => o.Replace("日志内容：", "") + ";").ToArray();
            //Accordtxt(sw.ElapsedMilliseconds);
            //File.WriteAllLines("Log2021-12-01-2.txt", list1);
            //string str = File.ReadAllText("test.ini");
            //string str1 =JObject.Parse(str)["params"]["guid"].ToString();
            //string test = IcyShell.getCmdExec("tune2fs", " -l /dev/sda1 |grep 'UUID'")[0];




            //string content = File.ReadAllText("D:/1.json");
            //JObject obj = JObject.Parse(content);
            //JObject picObj = JObject.Parse(obj["pic1"].ToString());
            //string base1 = picObj["proofImage"].ToString();

            ////SavePic(base1, "D:/", "1.jpg");
            //SavePic(obj["pic2"].ToString(), "D:/", "2.jpg");
            //SavePic(obj["pic3"].ToString(), "D:/", "3.jpg");
            //string url = "https://www.baidu.com/";
            //string reContent = IcyHttp.WebApiPost(url, "", 10);
            //Accordtxt(reContent);
            //string[] strarr = reContent.Split('\n');
            //Process[] process = Process.GetProcesses();
            //for (int i = 0; i < process.Length; i++)
            //{
            //    Accordtxt(process[i].ProcessName);
            //}
            //GetPicThumbnail("Json/checkpage_pic2.jpg", "Json/test.jpg", 50);


            //string str1 = "뛀䡅倱㘱";
            //byte[] buf = System.Text.Encoding.Unicode.GetBytes(str1);
            //string result = System.Text.Encoding.GetEncoding("gb2312").GetString(buf, 0, buf.Length);
            //Accordtxt(result);

            //string str ="{\"page_no\": 1,\"page_size\": 8,\"export\":true}";
            //string url = "http://10.8.0.58:8888/qinbin_server/checkpage/get_history";
            //string reContent = IcyHttp.WebApiPost(url, str, 10);
            //int i = Environment.TickCount;
            //Accordtxt(int.MaxValue/3600000/24);


            //byte[] bt=StrToHexByte(textBox2.Text);
            //Accordtxt(byteToHexStr(CRC16(bt)));
            //bool connStatus = DBService.testCon("MYSQL", "127.0.0.1", "root", "12345", "ElectTollData", 5);
            //if (!connStatus)
            //{
            //    MessageBox.Show("数据库连接失败！");
            //    return;
            //}
            //string sql = string.Format("INSERT INTO server_basic info (date,net,det,sum) VALUES ('{0}',{1},{2},{3}) ON DUPLICATE KEY UPDATE net= values(net),sum= values(sum),det= values(det)", DateTime.Now.ToString("yyyy-MM-dd"));
            //if (!DBService.Execute(sql))
            //{

            //}
            //Accordtxt(json);
            //Dictionary<string, object> dt = new Dictionary<string, object>();
            //List<List<int>> list_ = new List<List<int>>();
            //dt.Add("test", list_);
            //dt.Add("test1", 1);
            //dt.Add("test2", "1.1");
            //string s = IcyJson.ToJsonStr(dt);
            //Accordtxt("over");
            //for (int i=0;i<4;i++)
            //{
            //    ThreadPool.QueueUserWorkItem(ThreadNum, i);
            //}
            //Thread.Sleep(1000);
            //Accordtxt(string.Join(",", test_num));
        }
        int[] test_num = new int[4];
        void ThreadNum(object obj)
        {
            int index=(int)obj;
            for (int i=0;i<1000;i++)
            {
                test_num[index]++;
            }
        }
          byte[] CRC16 ( byte[] puchMsg)  /* 函数以 unsigned short 类型返回 CRC */
          {
                byte uchCRCHi = 0xFF ;  /* CRC 的高字节初始化 */
                byte uchCRCLo = 0xFF ;  /* CRC 的低字节初始化 */
                int uIndex ;  /* CRC 查询表索引 */
                byte[] byteSend = new byte[puchMsg.Length + 2];
                for (int i = 0; i < puchMsg.Length; i++)
                {
                        uIndex = uchCRCLo ^ puchMsg[i] ; /* 计算 CRC */
                        uchCRCLo = (byte)(uchCRCHi ^ auchCRCHi[uIndex]);
                        uchCRCHi = auchCRCLo[uIndex] ;
                        byteSend[i] = puchMsg[i];
                }
                byteSend[puchMsg.Length ] = uchCRCLo;
                byteSend[puchMsg.Length + 1] = uchCRCHi;
                return byteSend;
        }
       byte[] auchCRCHi= {
                                    0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
                                    0x00, 0xC1, 0x81,
                                    0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81,
                                    0x40, 0x01, 0xC0,
                                    0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1,
                                    0x81, 0x40, 0x01,
                                    0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01,
                                    0xC0, 0x80, 0x41,
                                    0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
                                    0x00, 0xC1, 0x81,
                                    0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80,
                                    0x41, 0x01, 0xC0,
                                    0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
                                    0x80, 0x41, 0x01,
                                    0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00,
                                    0xC1, 0x81, 0x40,
                                    0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
                                    0x00, 0xC1, 0x81,
                                    0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
                                    0x40, 0x01, 0xC0,
                                    0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1,
                                    0x81, 0x40, 0x01,
                                    0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01,
                                    0xC0, 0x80, 0x41,
                                    0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
                                    0x00, 0xC1, 0x81,
                                    0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
                                    0x40, 0x01, 0xC0,
                                    0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
                                    0x80, 0x41, 0x01,
                                    0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01,
                                    0xC0, 0x80, 0x41,
                                    0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
                                    0x00, 0xC1, 0x81,
                                    0x40
                                    } ;
                                        /* 低位字节的 CRC 值 */
                                    byte[] auchCRCLo = {
                                    0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06, 0x07, 0xC7,
                                    0x05, 0xC5, 0xC4,
                                    0x04, 0xCC, 0x0C, 0x0D, 0xCD, 0x0F, 0xCF, 0xCE, 0x0E, 0x0A, 0xCA, 0xCB,
                                    0x0B, 0xC9, 0x09,
                                    0x08, 0xC8, 0xD8, 0x18, 0x19, 0xD9, 0x1B, 0xDB, 0xDA, 0x1A, 0x1E, 0xDE,
                                    0xDF, 0x1F, 0xDD,
                                    0x1D, 0x1C, 0xDC, 0x14, 0xD4, 0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2,
                                    0x12, 0x13, 0xD3,
                                    0x11, 0xD1, 0xD0, 0x10, 0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3, 0xF2, 0x32,
                                    0x36, 0xF6, 0xF7,
                                    0x37, 0xF5, 0x35, 0x34, 0xF4, 0x3C, 0xFC, 0xFD, 0x3D, 0xFF, 0x3F, 0x3E,
                                    0xFE, 0xFA, 0x3A,
                                    0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38, 0x28, 0xE8, 0xE9, 0x29, 0xEB, 0x2B,
                                    0x2A, 0xEA, 0xEE,
                                    0x2E, 0x2F, 0xEF, 0x2D, 0xED, 0xEC, 0x2C, 0xE4, 0x24, 0x25, 0xE5, 0x27,
                                    0xE7, 0xE6, 0x26,
                                    0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0, 0xA0, 0x60, 0x61, 0xA1,
                                    0x63, 0xA3, 0xA2,
                                    0x62, 0x66, 0xA6, 0xA7, 0x67, 0xA5, 0x65, 0x64, 0xA4, 0x6C, 0xAC, 0xAD,
                                    0x6D, 0xAF, 0x6F,
                                    0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB, 0x69, 0xA9, 0xA8, 0x68, 0x78, 0xB8,
                                    0xB9, 0x79, 0xBB,
                                    0x7B, 0x7A, 0xBA, 0xBE, 0x7E, 0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C, 0xB4,
                                    0x74, 0x75, 0xB5,
                                    0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71, 0x70, 0xB0,
                                    0x50, 0x90, 0x91,
                                    0x51, 0x93, 0x53, 0x52, 0x92, 0x96, 0x56, 0x57, 0x97, 0x55, 0x95, 0x94,
                                    0x54, 0x9C, 0x5C,
                                    0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E, 0x5A, 0x9A, 0x9B, 0x5B, 0x99, 0x59,
                                    0x58, 0x98, 0x88,
                                    0x48, 0x49, 0x89, 0x4B, 0x8B, 0x8A, 0x4A, 0x4E, 0x8E, 0x8F, 0x4F, 0x8D,
                                    0x4D, 0x4C, 0x8C,
                                    0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42, 0x43, 0x83,
                                    0x41, 0x81, 0x80,
                                    0x40
                                    };
        void test()
        {
            UnLawfulData data = new UnLawfulData();
            data.RecordId = "123";
            data.RelateEtcObuVehicleUserType = "A";
            bool connStatus = DBService.testCon("MYSQL", "127.0.0.1", "root", "12345", "ElectTollData", 5);
            if (!connStatus)
            {
                MessageBox.Show("数据库连接失败！");
                return;
            }

            string sql = InsertSqlCreat(getUnLawfulDataName(), data);
            if (!DBService.Execute(sql))
            {
                string tempsql = create_sql.Replace("UnLawfulData", getUnLawfulDataName());
                DBService.Execute(tempsql);
                DBService.Execute(sql);
            }

            string json = JsonConvert.SerializeObject(data);
            UnLawfulData jsonObj = JsonConvert.DeserializeObject<UnLawfulData>(json);
            Accordtxt(jsonObj.PicPath1);
        }
        public static string getUnLawfulDataName()
        {
            return "UnLawfulData" + DateTime.Now.Year;
        }

        string create_sql= @"CREATE TABLE IF NOT EXISTS `UnLawfulData`(
                                        `ID` int(11) NOT NULL  AUTO_INCREMENT,
                                        `RecordId` varchar(42) DEFAULT NULL,
                                        `RecordType` varchar(1) DEFAULT NULL,
                                        `UnlawfulMold` varchar(50) DEFAULT NULL,
                                        `LaneNo` tinyint DEFAULT NULL,
                                        `DetectTime` datetime(3) DEFAULT NULL,
                                        `CaptureTime` datetime(3) DEFAULT NULL,
                                        `ProofTime` datetime(3) DEFAULT NULL,
                                        `DistPlate` varchar(10) DEFAULT NULL,
                                        `HistPlate` varchar(10) DEFAULT NULL,
                                        `DistPlateColor` smallint DEFAULT NULL,
                                        `HistPlateColor` smallint DEFAULT NULL,
                                        `DistVehicleClass` smallint DEFAULT NULL,
                                        `HistVehicleClass` smallint DEFAULT NULL,
                                        `HistVehicleUserType` smallint DEFAULT NULL,
                                        `VehicleSpeed` smallint DEFAULT NULL,
                                        `FinalTransStatus` smallint DEFAULT NULL,
                                        `UsedHistEtcInfo` varchar(20) DEFAULT NULL,

                                        `EtcMac` VARCHAR(8) DEFAULT NULL,
                                        `EtcContractSerial` VARCHAR(16) DEFAULT NULL,
                                        `EtcTransTime` datetime(3) DEFAULT NULL,
                                        `EtcTradeResult` varchar(4) DEFAULT NULL,
                                        `EtcStatus` varchar(4) DEFAULT NULL,
                                        `EtcObuSignDate` datetime(3) DEFAULT NULL,
                                        `EtcObuExpireDate` datetime(3) DEFAULT NULL,
                                        `EtcObuGeneralState` smallint DEFAULT NULL,
                                        `EtcObuEntryTime` datetime(3) DEFAULT NULL,
                                        `EtcObuPlate`	varchar(10) DEFAULT NULL,
                                        `EtcObuPlateColor` smallint DEFAULT NULL,
                                        `EtcObuVehicleClass` smallint	DEFAULT NULL,
                                        `EtcObuVehicleUserType` smallint DEFAULT NULL,
                                        `EtcIccSignDate` datetime(3) DEFAULT NULL,
                                        `EtcIccExpireDate` datetime(3) DEFAULT NULL,
                                        `EtcIccGeneralState` smallint DEFAULT NULL,
                                        `EtcIccEntryTime` datetime(3) DEFAULT NULL,
                                        `EtcIccPlate` varchar(10) DEFAULT NULL,
                                        `EtcIccPlateColor`smallint	DEFAULT NULL,
                                        `EtcIccVehicleClass` smallint DEFAULT NULL,
                                        `EtcIccVehicleUserType`smallint	DEFAULT NULL,
                                        `EtcValidSign`	varchar(4)	DEFAULT NULL,

                                        `RelateEtcMac` varchar(255) DEFAULT NULL,
                                        `RelateEtcContractSerial` varchar(255) DEFAULT NULL,
                                        `RelateEtcTransTime` varchar(255) DEFAULT NULL,
                                        `RelateEtcTradeResult` varchar(255) DEFAULT NULL,
                                        `RelateEtcStatus` varchar(255) DEFAULT NULL,
                                        `RelateEtcObuSignDate` varchar(255) DEFAULT NULL,
                                        `RelateEtcObuExpireDate` varchar(255) DEFAULT NULL,
                                        `RelateEtcObuGeneralState` varchar(255) DEFAULT NULL,
                                        `RelateEtcObuEntryTime` varchar(255) DEFAULT NULL,
                                        `RelateEtcObuPlate` varchar(255) DEFAULT NULL,
                                        `RelateEtcObuPlateColor` varchar(255) DEFAULT NULL,
                                        `RelateEtcObuVehicleClass` varchar(255)	DEFAULT NULL,
                                        `RelateEtcObuVehicleUserType` varchar(255) DEFAULT NULL,
                                        `RelateEtcIccSignDate` varchar(255) DEFAULT NULL,
                                        `RelateEtcIccExpireDate` varchar(255) DEFAULT NULL,
                                        `RelateEtcIccGeneralState` varchar(255)	DEFAULT NULL,
                                        `RelateEtcIccEntryTime` varchar(255) DEFAULT NULL,
                                        `RelateEtcIccPlate` varchar(255) DEFAULT NULL,
                                        `RelateEtcIccPlateColor`varchar(255)	DEFAULT NULL,
                                        `RelateEtcIccVehicleClass` varchar(255) DEFAULT NULL,
                                        `RelateEtcIccVehicleUserType`varchar(255) DEFAULT NULL,
                                        `RelateEtcValidSign` varchar(255)	DEFAULT NULL,


                                        `PLaserId` smallint DEFAULT NULL, 
                                        `PLaserTriggerTime` datetime(3) DEFAULT NULL,
                                        `CLaserId` smallint DEFAULT NULL, 
                                        `CLaserTouchTime` datetime(3) DEFAULT NULL,
                                        `CLaserVehicleClass` smallint DEFAULT NULL,
                                        `CameraId` smallint DEFAULT NULL,
                                        `CameraPictureTime`	datetime(3) DEFAULT NULL,
                                        `CameraPlate` varchar(10) DEFAULT NULL,
                                        `CameraPlateColor` smallint DEFAULT NULL,

                                        `PicPath1` varchar(255) DEFAULT NULL,
                                        `PicPath2` varchar(255) DEFAULT NULL,
                                        `PicPath3` varchar(255) DEFAULT NULL,
                                        `PicPath4` varchar(255) DEFAULT NULL,
                                        `PicPath5` varchar(255) DEFAULT NULL,
                                        `PicPath6` varchar(255) DEFAULT NULL,
                                        `PicPath7` varchar(255) DEFAULT NULL,
                                        `PicPath8` varchar(255) DEFAULT NULL,
                                        `Reserve1` int(11) DEFAULT NULL,
                                        `Reserve2` int(11) DEFAULT NULL,
                                        `Reserve3` int(11) DEFAULT NULL,
                                        `Reserve4` int(11) DEFAULT NULL,
                                        `ModifyTime` datetime(0) NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP(0),
                                          PRIMARY KEY (`ID`),
                                          KEY `DetectTime` (`DetectTime`)
                                        ) ENGINE=InnoDB  AUTO_INCREMENT=1  DEFAULT CHARSET=utf8;";

        public class UnLawfulData
        {
            public string RecordId { get; set; }
            public string RecordType { get; set; }
            public string UnlawfulMold { get; set; }
            public int LaneNo { get; set; }
            public DateTime DetectTime { get; set; }
            public DateTime CaptureTime { get; set; }
            public DateTime ProofTime { get; set; }
            public string DistPlate { get; set; }
            public string HistPlate { get; set; }
            public int DistPlateColor { get; set; }
            public int HistPlateColor { get; set; }
            public int DistVehicleClass { get; set; }
            public int HistVehicleClass { get; set; }
            public int HistVehicleUserType { get; set; }
            public int VehicleSpeed { get; set; }
            public int FinalTransStatus { get; set; }
            public string UsedHistEtcInfo { get; set; }

            public string EtcMac { get; set; }
            public string EtcContractSerial { get; set; }
            public DateTime EtcTransTime { get; set; }
            public string EtcTradeResult { get; set; }
            public string EtcStatus { get; set; }
            public DateTime EtcObuSignDate { get; set; }
            public DateTime EtcObuExpireDate { get; set; }
            public int EtcObuGeneralState { get; set; }
            public DateTime EtcObuEntryTime { get; set; }
            public string EtcObuPlate { get; set; }
            public int EtcObuPlateColor { get; set; }
            public int EtcObuVehicleClass { get; set; }
            public int EtcObuVehicleUserType { get; set; }
            public DateTime EtcIccSignDate { get; set; }
            public DateTime EtcIccExpireDate { get; set; }
            public int EtcIccGeneralState { get; set; }
            public DateTime EtcIccEntryTime { get; set; }
            public string EtcIccPlate { get; set; }
            public int EtcIccPlateColor { get; set; }
            public int EtcIccVehicleClass { get; set; }
            public int EtcIccVehicleUserType { get; set; }
            public string EtcValidSign { get; set; }



            public string RelateEtcMac { get; set; }
            public string RelateEtcContractSerial { get; set; }
            public string RelateEtcTransTime { get; set; }
            public string RelateEtcTradeResult { get; set; }
            public string RelateEtcStatus { get; set; }
            public string RelateEtcObuSignDate { get; set; }
            public string RelateEtcObuExpireDate { get; set; }
            public string RelateEtcObuGeneralState { get; set; }
            public string RelateEtcObuEntryTime { get; set; }
            public string RelateEtcObuPlate { get; set; }
            public string RelateEtcObuPlateColor { get; set; }
            public string RelateEtcObuVehicleClass { get; set; }
            public string RelateEtcObuVehicleUserType { get; set; }
            public string RelateEtcIccSignDate { get; set; }
            public string RelateEtcIccExpireDate { get; set; }
            public string RelateEtcIccGeneralState { get; set; }
            public string RelateEtcIccEntryTime { get; set; }
            public string RelateEtcIccPlate { get; set; }
            public string RelateEtcIccPlateColor { get; set; }
            public string RelateEtcIccVehicleClass { get; set; }
            public string RelateEtcIccVehicleUserType { get; set; }
            public string RelateEtcValidSign { get; set; }


            public int PLaserId { get; set; }
            public DateTime PLaserTriggerTime { get; set; }
            public int CLaserId { get; set; }
            public DateTime CLaserTouchTime { get; set; }
            public int CLaserVehicleClass { get; set; }
            public int CameraId { get; set; }
            public DateTime CameraPictureTime { get; set; }
            public string CameraPlate { get; set; }
            public int CameraPlateColor { get; set; }
            public string PicPath1 { get; set; }
            public string PicPath2 { get; set; }
            public string PicPath3 { get; set; }
            public string PicPath4 { get; set; }
            public string PicPath5 { get; set; }
            public string PicPath6 { get; set; }
            public string PicPath7 { get; set; }
            public string PicPath8 { get; set; }
        }

        public static string InsertSqlCreat(string table_name,Object obj)
        {
            Type type = obj.GetType();
            PropertyInfo[] propertys = type.GetProperties();
            string[] col = new string[propertys.Length];
            object[] value = new object[propertys.Length];
            for (int i = 0; i < col.Length; i++)
            {
                col[i] = propertys[i].Name;//拿到属性名称
                if (propertys[i].PropertyType == typeof(DateTime))
                {
                    value[i] = Convert.ToDateTime(propertys[i].GetValue(obj)).ToString("yyyy-MM-dd HH:mm:ss.fff");
                }
                else
                {
                    value[i] = propertys[i].GetValue(obj);//拿到属性的值
                }
            }
            StringBuilder sbsql = new StringBuilder();
            sbsql.Append("INSERT INTO ").Append(table_name).Append("(").Append(string.Join(",",col)).Append(")").Append("VALUES ('").Append(string.Join("','", value)).Append("')");
            string retstr=sbsql.ToString();
            return retstr;
        }

        private void Btn_Stop_Click(object sender, EventArgs e)
        {
            for (int i=0;i<list_thread.Count;i++)
            {
                try
                {
                    list_thread[i].Abort();
                }
                catch
                {
                	    
                }
            }
            Accordtxt("终止发送");
            list_thread.Clear();
            send_file.Clear();
        }

        private void button_Ssh_Cmd_Click(object sender, EventArgs e)
        {
            string file_filter = "文件(*.ini)|*.ini|所有文件|*.*";
            DialogSelect(file_filter, Ssh_Cmd);
        }
        void Ssh_Cmd(OpenFileDialog dialog)
        {
            try
            {
                string[] cmd_lines = File.ReadAllLines(dialog.FileName, Encoding.UTF8);
                string ip = textBox_Ssh_IP.Text.Trim();
                string password = textBox_Ssh_Password.Text.Trim();
                if (cmd_lines.Length == 0)
                {
                    Accordtxt("空文件");
                    return;
                }
                using (var sshClient = new SshClient(ip, 22, "root", password))
                {
                    sshClient.Connect();
                    for (int i = 0; i < cmd_lines.Length; i++)
                    {
                        if (cmd_lines[i].Trim() == "")
                        {
                            continue;
                        }
                        string cmd = cmd_lines[i].Trim();
                        var result = sshClient.RunCommand(cmd).Result;
                        Accordtxt(cmd);
                        Accordtxt(result);
                    }
                    sshClient.Disconnect();
                    Accordtxt("执行结束");
                }
            }
            catch(Exception ex)
            {
                Accordtxt(ex.Message);
            }
        }

        private void Btn_CloudSend_Click(object sender, EventArgs e)
        {
               IcyThread.Start(ThreadPost, "Json/Retry/");
        }
        public  void ThreadPost(object _filepath)//串行http发送
        {
            string reContent = "";//http返回内容
            FileSystemInfo FirstFile = null;
            string filepath = (string)_filepath;
            while (true)
            {
                try
                {
                    var dir = new DirectoryInfo(filepath);
                    if (!dir.Exists)
                    {
                        dir.Create();
                    }
                    //FileInfo[] fi = dir.GetFiles().OrderByDescending(f => f.LastWriteTime).ToArray();//时间倒序
                    FileInfo[] fi = dir.GetFiles();
                    int error_num = 0;
                    int max_num = fi.Length > 100 ? 100 : fi.Length;//最多循环100次，开启下一次读取，尽量保证新数据的成功发送
                    for (int i = 0; i < max_num; i++)
                    {
                        FirstFile = fi[i];
                        if (error_num > 10)//单次循环发送失败数超过10个时，启动超时时间清除判断，之后重新开始读取、发送
                        {
                            for (int j = fi.Length - 1; j >= 0; j--)
                            {
                                FirstFile = fi[j];
                                if (FirstFile.LastWriteTime < DateTime.Now.AddDays(-1))//一天前数据删除，是该文件夹下最多保持一天数据
                                {
                                    File.Delete(FirstFile.FullName);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            break;
                        }
                        string JsonContent = File.ReadAllText(FirstFile.FullName);
                        string URL = "";
                        if (filepath == "Json/CameraPost/")
                        {
                            //URL = EGlobal.ServerUrl + FirstFile.Name;
                        }
                        else
                        {
                            //URL = EGlobal.DataServerUrl + FirstFile.Name;
                            URL = txb_url.Text + FirstFile.Name;
                        }
                        reContent = IcyHttp.WebApiPost(URL, JsonContent,30);
                        if (!string.IsNullOrEmpty(reContent) && !reContent.StartsWith("Error"))
                        {
                            Log.WriteLog("Post", filepath, "Name:" + FirstFile.Name);
                            File.Delete(FirstFile.FullName);
                        }
                        else
                        {
                            error_num++;
                            Log.WriteLog("PostError", filepath, reContent + "\n" + FirstFile.Name + "\n" + URL);
                            Thread.Sleep(1000);
                        }
                    }
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    Log.WriteLog("PostError", filepath, ex.Message);
                    Thread.Sleep(1000);
                }
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.InitialDirectory = System.Environment.CurrentDirectory;
            dialog.Filter = "文件(*.txt;*.csv)|*.txt;*.csv|所有文件|*.*";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string path = dialog.FileName;
                    string image = System.IO.File.ReadAllText(path);
                    var bytes = Convert.FromBase64String(image);
                    using (var imageFile = new FileStream("Json/test.txt", FileMode.Create))
                    {
                        imageFile.Write(bytes, 0, bytes.Length);
                        imageFile.Flush();
                    }
                    using (var imageFile = new FileStream("Json/test.jpg", FileMode.Create))
                    {
                        imageFile.Write(bytes, 0, bytes.Length);
                        imageFile.Flush();
                    }
                }
                catch (System.Exception ex)
                {
                    Accordtxt(ex.Message);
                }
            }

            //Accordtxt(showstr1);
            //string ip = "10.100.16.109";
            //string password = "wanji@300552";
            //using (var sftpClient = new SftpClient(ip, 22, "root", password))
            //{
            //    sftpClient.Connect();
            //    var name = sftpClient.ListDirectory("/home/Debug").Select(s => s.Name).Where(s => s.EndsWith("dll")).ToList();
            //   name= name.OrderBy(o => o).ToList ();
            //    //Accordtxt(string.Join(" ",name));
            //}
            //try
            //{
            //    string[] atlas_arr = { "10.66.26.13", "10.66.26.14" };
            //    string password = "Huawei@SYS3";
            //    for (int i = 0; i < atlas_arr.Length; i++)
            //    {
            //        List<string> pic_name = new List<string>();
            //        using (var sshClient = new SshClient(atlas_arr[i], 22, "root", password))
            //        {
            //            sshClient.Connect();
            //            string cmd = "ls /etc_data/PicShare/2021110311/";
            //            string result = sshClient.RunCommand(cmd).Result;
            //            Accordtxt(result);
            //            pic_name.AddRange(result.Split('\n'));
            //            sshClient.Disconnect();
            //        }
            //        pic_name = pic_name.OrderBy(o => o).ToList();
            //        File.WriteAllLines("data/" + atlas_arr[i].Replace(".", "_") + ".csv", pic_name);
            //    }
            //    Accordtxt("数据获取完成");
            //}
            //catch (Exception)
            //{

            //    throw;
            //}


            //SendInfoToSpecifiedMailbox("wangzikun@wanji.net.cn", "浙江大系统异常_"+DateTime.Now, "TEST");
        }

        string SendMailBox = "592147313@qq.com";
        string Host = "smtp.qq.com";

        /// <summary>
        /// 向指定邮箱发送信息
        /// </summary>
        /// <param name="receivingMailbox">接收者邮箱</param>
        /// <param name="subject">邮件主题</param>
        /// <param name="mailContent">邮件内容</param>
        public void SendInfoToSpecifiedMailbox(string receivingMailbox, string subject, string mailContent)
        {
            //SendMailbox:发送信息的邮箱
            //SMIPServiceCode:邮箱smtp服务密码，确保邮箱已经开启了SMTP服务，开启后会给出一串编码就是smtp服务密码，后台填入编码
            //Host:邮箱服务器类型,我这里用的是QQ邮箱：smtp.qq.com
            //Port:邮箱服务器端口
            SmtpClient client = new SmtpClient(Host, 587);
            MailMessage msg = new MailMessage(SendMailBox, receivingMailbox, subject, mailContent);
            client.UseDefaultCredentials = false;
            System.Net.NetworkCredential basicAuthenticationInfo =
            new System.Net.NetworkCredential(SendMailBox, "tltvbvotsiolbcjf");
            client.Credentials = basicAuthenticationInfo;
            client.EnableSsl = true;
            client.Send(msg);
        }

        void JsonDeal(object filename)
        {
            string fileName = (string)filename;
            string body = File.ReadAllText(fileName);
            EdgeUnLawfulData d1 = JsonConvert.DeserializeObject<EdgeUnLawfulData>(body);
            if (d1.unlawfulMold == "")
            {
                d1.pic1 = "";
                d1.pic2 = "";
                d1.pic3 = "";
                File.WriteAllText(fileName, IcyJson.ToJsonStr(d1));
            }
        }
        public class EdgeUnLawfulData//门架端上传数据解析
        {
            public string recordId { get; set; }
            public string recordType { get; set; }
            public string unlawfulMold { get; set; }
            public int laneNo { get; set; }
            public string detectTime { get; set; }


            public string etcMac { get; set; }
            public string etcContractSerial { get; set; }
            public string etcTradeResult { get; set; }
            public string etcLicense { get; set; }
            public int etcLicenseColor { get; set; }
            public int etcVehClass { get; set; }
            public string etcStatus { get; set; }
            public string obuTollStation { get; set; }
            public string obuEntryTime { get; set; }
            public int finalTradeResult { get; set; }
            //关联etc
            public string relateEtcMac { get; set; }
            //相机
            public string picLicense { get; set; }
            public int picLicenseColor { get; set; }

            //车型识别
            public int proofVehClass { get; set; }

            public string pic1 { get; set; }
            public string pic2 { get; set; }
            public string pic3 { get; set; }
        }
        public static void Send163Email(string Receiver, string Subject, string content)
        {
            //163邮箱发送配置                   
            var client = new System.Net.Mail.SmtpClient();
            client.Host = "smtp.163.com";
            client.Port = 25;
            client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
            client.EnableSsl = true;
            client.UseDefaultCredentials = true;
            client.Credentials = new System.Net.NetworkCredential("luoye18813168650@163.com", "ZNYWJGEVYZZUJMKB");
            System.Net.Mail.MailMessage Message = new System.Net.Mail.MailMessage();
            Message.SubjectEncoding = System.Text.Encoding.UTF8;
            Message.BodyEncoding = System.Text.Encoding.UTF8;
            Message.Priority = System.Net.Mail.MailPriority.High;
            Message.From = new System.Net.Mail.MailAddress("luoye18813168650@163.com");
            //添加邮件接收人地址
            string[] receivers = Receiver.Split(new char[] { ',' });
            Array.ForEach(receivers.ToArray(), ToMail => { Message.To.Add(ToMail); });
            Message.Subject = Subject;
            Message.Body = content;
            Message.IsBodyHtml = true;
            client.Send(Message);
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button11_Click(object sender, EventArgs e)
        {
            string file_filter = "文件(*.jpg)|*.jpg|所有文件|*.*";
            DialogSelect(file_filter, FindPlate);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            DirectoryInfo theFolder = new DirectoryInfo(textBox_location.Text+"/images");
            DirectoryInfo[] dirInfo = theFolder.GetDirectories();//获取所在目录的文件夹
            FileInfo[] file = theFolder.GetFiles();//获取所在目录的文件
            string fileName = "";

            foreach (FileInfo fileItem in file) //遍历文件
            {
                try
                {
                    fileName = fileItem.Name.Replace(".jpg",".txt");
                    if (!File.Exists(textBox_location.Text + "/labels/"+ fileName))
                    {
                        Accordtxt(fileName+"不存在");
                        File.Delete(fileItem.FullName);
                    }
                }
                catch (System.Exception ex)
                {
                    Log.WriteLog("Error", "ForeachFile", fileName + "\n" + ex.ToString());
                }
            }


            theFolder = new DirectoryInfo(textBox_location.Text + "/labels");
            dirInfo = theFolder.GetDirectories();//获取所在目录的文件夹
            file = theFolder.GetFiles();//获取所在目录的文件

            foreach (FileInfo fileItem in file) //遍历文件
            {
                try
                {
                    fileName = fileItem.Name.Replace(".txt", ".jpg");
                    if (!File.Exists(textBox_location.Text + "/images/" + fileName))
                    {
                        Accordtxt(fileName + "不存在");
                        File.Delete(fileItem.FullName);
                    }
                }
                catch (System.Exception ex)
                {
                    Log.WriteLog("Error", "ForeachFile", fileName + "\n" + ex.ToString());
                }
            }

            Accordtxt("清洗结束");
        }

        private void button18_Click(object sender, EventArgs e)
        {
            double year = double.Parse(tbx_year.Text);
            double rate = double.Parse(tbx_rate.Text);
            double pe_10y = double.Parse(tbx_pe10y.Text);
            double pe_now = double.Parse(tbx_pe_now.Text);
            double sum = 0;
            for (int i = 0; i < year; i++)
            {
                sum += Math.Pow(1 + rate, i);
            }
            double result = Math.Pow((sum + Math.Pow(1 + rate, year - 1) * pe_10y) / pe_now, 1 / year);
            tbx_10rate.Text = (result - 1).ToString("P2");

            double[] rate_arr = { 0.03, 0.05, 0.08, 0.1, 0.12, 0.13, 0.14, 0.15, 0.2, 0.25, 0.3 };
            string str = "\n";
            for (int i = 0; i < rate_arr.Length; i++)
            {
                str += rate_arr[i].ToString("P0") + "  " + Math.Pow(1 + rate_arr[i], 10).ToString("0.00")+"\n";
            }
            Accordtxt(str);
        }

        private void button17_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                dialog.Description = "请选择文件路径";
                dialog.SelectedPath = "D:/Copy/";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string str = dialog.SelectedPath;
              
                    DateTime t1 = DateTime.Now;
                    ForeachFileCopy(str);
                    DateTime t2 = DateTime.Now;
                    Accordtxt((t2 - t1).TotalMilliseconds.ToString() + " ms  over");
                    //Accordtxt(task_list.Count + "个Json提取完成，请在照片目录下查看");
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void ForeachFileCopy(string filePathByForeach)
        {
            DirectoryInfo theFolder = new DirectoryInfo(filePathByForeach);
            DirectoryInfo[] dirInfo = theFolder.GetDirectories();//获取所在目录的文件夹
            FileInfo[] file = theFolder.GetFiles();//获取所在目录的文件
            string fileName = "";

            foreach (FileInfo fileItem in file) //遍历文件
            {
                try
                {
                    fileName = fileItem.FullName;
                    if (fileName.EndsWith(".cs") || fileName.EndsWith(".resx") || fileName.EndsWith(".sln") || fileName.EndsWith(".suo") || fileName.EndsWith(".csproj"))
                    {
                        var str = File.ReadAllBytes(fileName);
                        string file_copy = fileName.Replace(fileItem.Extension, fileItem.Extension + ".ini");
                        File.WriteAllBytes(file_copy, str);
                        File.Delete(fileName);
                    }
                    //if (fileItem.Extension==".ini")
                    //{
                    //    string file_rename = fileName.Replace(fileItem.Extension, "");
                    //    File.Move(fileName, file_rename);
                    //}
                }
                catch (System.Exception ex)
                {
                    Log.WriteLog("Error", "ForeachFile", fileName + "\n" + ex.ToString());
                }
            }
            //遍历文件夹
            foreach (DirectoryInfo NextFolder in dirInfo)
            {
                ForeachFileCopy(NextFolder.FullName);
            }
        }

        private void button19_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                if (path == "")
                {
                    dialog.InitialDirectory = System.Environment.CurrentDirectory + "/";
                }
                else
                {
                    dialog.InitialDirectory = path;
                }
                dialog.Filter = "文件(*.*)|*.*|所有文件|*.*";
                dialog.ValidateNames = true;
                dialog.CheckPathExists = true;
                dialog.CheckFileExists = true;
                dialog.Multiselect = true;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    path = dialog.FileNames[0];
                    string fileName = "";
                    DateTime t1 = DateTime.Now;
                    for (int i = 0; i < dialog.FileNames.Length; i++)
                    {
                        fileName = dialog.FileNames[i];
                        try
                        {
                            var str = File.ReadAllBytes(fileName);
                            FileInfo fileItem = new FileInfo(fileName);
                            string file_copy = fileName.Replace(fileItem.Extension, fileItem.Extension + ".ini");
                            File.WriteAllBytes(file_copy, str);
                            //File.Copy(file_copy, fileName, true);
                            //task_list.Add(Task.Factory.StartNew(PicCatch, fileName));
                        }
                        catch (System.Exception ex)
                        {
                            Accordtxt(fileName + "\n" + ex.ToString());
                        }
                    }
                    DateTime t2 = DateTime.Now;
                    Accordtxt((t2 - t1).TotalMilliseconds.ToString() + "ms");

                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void button20_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                dialog.Description = "请选择文件路径";
                dialog.SelectedPath = "D:/Copy/";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string str = dialog.SelectedPath;

                    DateTime t1 = DateTime.Now;
                    Foreachini(str);
                    DateTime t2 = DateTime.Now;
                    Accordtxt((t2 - t1).TotalMilliseconds.ToString() + " ms  over");
                    //Accordtxt(task_list.Count + "个Json提取完成，请在照片目录下查看");
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void Foreachini(string filePathByForeach)
        {
            DirectoryInfo theFolder = new DirectoryInfo(filePathByForeach);
            DirectoryInfo[] dirInfo = theFolder.GetDirectories();//获取所在目录的文件夹
            FileInfo[] file = theFolder.GetFiles();//获取所在目录的文件
            string fileName = "";

            foreach (FileInfo fileItem in file) //遍历文件
            {
                try
                {
                    fileName = fileItem.FullName;
                    //if (fileName.Contains(".cs") || fileName.Contains(".resx") || fileName.Contains(".sln") || fileName.Contains(".suo") || fileName.Contains(".csproj"))
                    //{
                    //    var str = File.ReadAllBytes(fileName);
                    //    string file_copy = fileName.Replace(fileItem.Extension, fileItem.Extension + ".ini");
                    //    File.WriteAllBytes(file_copy, str);
                    //    File.Delete(fileName);
                    //}
                    if (fileItem.Extension == ".ini")
                    {
                        string file_rename = fileName.Replace(fileItem.Extension, "");
                        File.Move(fileName, file_rename);
                    }
                }
                catch (System.Exception ex)
                {
                    Log.WriteLog("Error", "ForeachFile", fileName + "\n" + ex.ToString());
                }
            }
            //遍历文件夹
            foreach (DirectoryInfo NextFolder in dirInfo)
            {
                Foreachini(NextFolder.FullName);
            }
        }
    }
}
