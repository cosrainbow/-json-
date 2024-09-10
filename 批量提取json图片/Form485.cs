using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;

namespace 批量提取json图片
{
    public partial class Form485 : Form
    {
        public Form485()
        {
            InitializeComponent();
        }


        private SerialPort g_Comm = new SerialPort();
        private SerialPort g_Comm2 = new SerialPort();
        private void Form1_Load(object sender, EventArgs e)
        {
            //初始化下拉串口名称列表框
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            comboPortName.Items.AddRange(ports);
            comboPortName.SelectedIndex = comboPortName.Items.Count > 0 ? 0 : -1;
            comboPortName2.Items.AddRange(ports);
            comboPortName2.SelectedIndex = comboPortName.Items.Count > 1 ? 1 : -1;
            comboBaudrate.SelectedIndex = comboBaudrate.Items.IndexOf("115200");
            comboCmd.SelectedIndex = comboCmd.Items.IndexOf("激光通信");
            comboAddress.SelectedIndex = comboAddress.Items.IndexOf("1");
            comboMode.SelectedIndex = comboMode.Items.IndexOf("纯485触发抓拍");

            //初始化SerialPort对象
            g_Comm.NewLine = "\r\n";
            g_Comm.RtsEnable = true;//根据实际情况吧。


            BTNSendData.Enabled = g_Comm.IsOpen;
            TXSendData.Enabled = true;

            //设置组帧的默认值
            TB_MAC1.Text = "08";
            TB_MAC2.Text = "FF";
            TB_MAC3.Text = "FF";
            TB_MAC4.Text = "FF";
            TB_PixelX11.Text = "500";
            TB_PixelY11.Text = "800";
            TB_PixelX12.Text = "900";
            TB_PixelY12.Text = "800";
            TB_LaserDealyTime.Text = "0";
            TB_CMD.Text = "1";
            TB_cameraDelay.Text = "0";

        }
        private void buttonOpenClose_Click(object sender, EventArgs e)
        {
            try
            {
                if (comboPortName.Text != "")
                {
                    //根据当前串口对象，来判断操作
                    if (g_Comm.IsOpen)
                    {
                        //打开时点击，则关闭串口
                        g_Comm.Close();
                    }
                    else
                    {
                        //关闭时点击，则设置好端口，波特率后打开
                        g_Comm.PortName = comboPortName.Text;
                        g_Comm.BaudRate = int.Parse(comboBaudrate.Text);
                        try
                        {
                            g_Comm.Open();
                        }
                        catch (Exception ex)
                        {
                            //捕获到异常信息，创建一个新的comm对象，之前的不能用了。
                            g_Comm = new SerialPort();
                            //现实异常信息给客户。
                            MessageBox.Show(ex.Message);
                        }
                    }
                }

                //设置按钮的状态
                buttonOpenClose.Text = g_Comm.IsOpen ? "关闭串口" : "打开串口";
                //           buttonSend.Enabled = comm.IsOpen;
                BTNSendData.Enabled = g_Comm.IsOpen;
                button3.Enabled = g_Comm.IsOpen;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public byte[] g_abSendData = new byte[250];
        public byte g_bSendDataLen = 0;
        public int carnum = 0;
        private void PackData_Click(object sender, EventArgs e)
        {
            string str_tmp = null;

            if (carnum > 0)
            {
                MessageBox.Show("不可重复添加！");
                return;
            }

            byte[] l_abPackData = new byte[50];
            char[] l_abTmpData = new char[10];
            byte l_bIndex = 0;

            int l_ncmd = int.Parse(comboCmd.SelectedIndex.ToString());
            l_abPackData[l_bIndex++] = (byte)(l_ncmd);//CMDType ,0设置命令/1-激光通信命令/2-天线通信命令

            int l_ntmp = int.Parse(comboAddress.SelectedIndex.ToString());
            l_abPackData[l_bIndex++] = (byte)(l_ntmp + 1);//485地址

            if (l_ncmd == 0)
            {
                l_ntmp = int.Parse(comboMode.SelectedIndex.ToString());
                l_abPackData[l_bIndex++] = (byte)(l_ntmp);//工作模式
            }
            else if ((l_ncmd == 1) || (l_ncmd == 2))
            {
                carnum = 1;
                l_abPackData[l_bIndex++] = 0x01;//车辆数，先默认为1

                Byte[] l_au8CBDTime = new Byte[8];

                String l_strSystemTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                get_BCDTime(l_au8CBDTime, l_strSystemTime);

                for (int n = 0; n < 8; n++)
                {
                    l_abPackData[l_bIndex++] = l_au8CBDTime[n];
                }

                str_tmp = TB_LaserDealyTime.Text.ToString();
                l_ntmp = int.Parse(str_tmp);
                l_abPackData[l_bIndex++] = (byte)l_ntmp;//激光延迟时间，默认40ms


                if (l_ncmd == 2)
                {
                    l_abPackData[l_bIndex++] = Convert.ToByte(TB_MAC1.Text, 16);
                    l_abPackData[l_bIndex++] = Convert.ToByte(TB_MAC2.Text, 16);
                    l_abPackData[l_bIndex++] = Convert.ToByte(TB_MAC3.Text, 16);
                    l_abPackData[l_bIndex++] = Convert.ToByte(TB_MAC4.Text, 16);
                }
                else
                {
                    str_tmp = textBox1.Text.ToString();
                    l_ntmp = int.Parse(str_tmp);
                    l_abPackData[l_bIndex++] = (byte)l_ntmp;//车辆ID，默认为0x08
                }

                str_tmp = TB_CMD.Text.ToString();
                l_ntmp = int.Parse(str_tmp);
                l_abPackData[l_bIndex++] = (byte)l_ntmp;//CMD,进行车牌识别，延迟输出识别结果



                str_tmp = TB_cameraDelay.Text.ToString();
                l_ntmp = int.Parse(str_tmp);
                l_abPackData[l_bIndex++] = (byte)(l_ntmp >> 8);//相机识别结果输出延时时间，高八位
                l_abPackData[l_bIndex++] = (byte)(l_ntmp - (byte)(l_ntmp >> 8) * 256);//相机识别结果输出延时时间，低八位



                str_tmp = TB_PixelX11.Text.ToString();
                l_ntmp = int.Parse(str_tmp);
                l_abPackData[l_bIndex++] = (byte)(l_ntmp >> 8);// 左上角X像素值，高八位
                l_abPackData[l_bIndex++] = (byte)(l_ntmp - (byte)(l_ntmp >> 8) * 256);// 左上角X像素值，低八位


                str_tmp = TB_PixelY11.Text.ToString();
                l_ntmp = int.Parse(str_tmp);
                l_abPackData[l_bIndex++] = (byte)(l_ntmp >> 8);// 左上角Y像素值，高八位
                l_abPackData[l_bIndex++] = (byte)(l_ntmp - (byte)(l_ntmp >> 8) * 256);// 左上角Y像素值，低八位


                str_tmp = TB_PixelX12.Text.ToString();
                l_ntmp = int.Parse(str_tmp);
                l_abPackData[l_bIndex++] = (byte)(l_ntmp >> 8);// 右上角X像素值，高八位
                l_abPackData[l_bIndex++] = (byte)(l_ntmp - (byte)(l_ntmp >> 8) * 256);// 右上角X像素值，低八位


                str_tmp = TB_PixelY12.Text.ToString();
                l_ntmp = int.Parse(str_tmp);
                l_abPackData[l_bIndex++] = (byte)(l_ntmp >> 8);// 右上角Y像素值，高八位
                l_abPackData[l_bIndex++] = (byte)(l_ntmp - (byte)(l_ntmp >> 8) * 256);// 右上角Y像素值，低八位
            }

            str_tmp = null;
            for (int l_ntmpi = 0; l_ntmpi < l_bIndex; l_ntmpi++)
            {
                str_tmp += l_abPackData[l_ntmpi].ToString("X2");
                str_tmp += " ";
            }
            TXSendData.Text = str_tmp;

            g_bSendDataLen = l_bIndex;

            DataCoding(g_abSendData, ref g_bSendDataLen);
        }

        private void get_BCDTime(Byte[] p_au8CBDTime, string p_strSystemTime)
        {
            int index = 0;
            p_au8CBDTime[index++] = (byte)((p_strSystemTime[2] - 48) * 16 + p_strSystemTime[3] - 48);      //年
            p_au8CBDTime[index++] = (byte)((p_strSystemTime[5] - 48) * 16 + p_strSystemTime[6] - 48);      //月
            p_au8CBDTime[index++] = (byte)((p_strSystemTime[8] - 48) * 16 + p_strSystemTime[9] - 48);      //日
            p_au8CBDTime[index++] = (byte)((p_strSystemTime[11] - 48) * 16 + p_strSystemTime[12] - 48);    //时
            p_au8CBDTime[index++] = (byte)((p_strSystemTime[14] - 48) * 16 + p_strSystemTime[15] - 48);    //分
            p_au8CBDTime[index++] = (byte)((p_strSystemTime[17] - 48) * 16 + p_strSystemTime[18] - 48);    //秒
            p_au8CBDTime[index++] = (byte)((p_strSystemTime[20] - 48) * 16 + p_strSystemTime[21] - 48);    //毫秒 -高
            p_au8CBDTime[index++] = (byte)((p_strSystemTime[22] - 48) * 16);                               //毫秒 -低
        }

        private void BTNSendData_Click(object sender, EventArgs e)
        {
            if (g_Comm.IsOpen)
            {
                string[] strdata = TXSendData.Text.Trim().Split(' ');
                byte[] l_abPackData = new byte[1024];
                byte l_bIndex = (byte)strdata.Length;
                for (int i = 0; i < strdata.Length; i++)
                {
                    l_abPackData[i] = Convert.ToByte(strdata[i], 16);
                }
                DataCoding(l_abPackData, ref l_bIndex);
                string str = DateTime.Now.ToString("hh:mm:ss.fff");
                g_Comm.Write(l_abPackData, 0, l_bIndex);
                MessageBox.Show(str, "触发时刻");
                //l_abPackData[3] = 0x02;
                //l_abPackData[l_bIndex - 2] ^= (0x01^0x02);
                //DataCoding(l_abPackData, ref l_bIndex);
                //g_Comm.Write(l_abPackData, 0, l_bIndex);
                //DateTime dt2 = DateTime.Now;
            }
            if (g_Comm2.IsOpen)
            {
                g_Comm2.Write(g_abSendData, 0, g_bSendDataLen);
            }
        }
        private void DataCoding(Byte[] buf, ref byte alen)
        {
            byte i, l_codelen;
            uint l_netsendlen = 0;
            Byte chk;
            Byte[] code_buf = new Byte[1024];

            /* ****************编码，加上起始标志、校验码和结束标志**************** */
            chk = 0;
            l_codelen = 0;
            code_buf[l_codelen++] = 0xff;
            code_buf[l_codelen++] = 0xff;
            for (i = 0; i < alen; i++)
            {
                //计算校验码
                chk ^= buf[i];
                //处理0xff特殊情况
                if (buf[i] == 0xff)
                {
                    code_buf[l_codelen++] = 0xfe;
                    code_buf[l_codelen++] = 0x01;
                }
                else if (buf[i] == 0xfe)
                {
                    code_buf[l_codelen++] = 0xfe;
                    code_buf[l_codelen++] = 0x00;
                }
                else
                    code_buf[l_codelen++] = buf[i];
            }

            if (chk == 0xff)
            {
                code_buf[l_codelen++] = 0xfe;
                code_buf[l_codelen++] = 0x01;
            }
            else if (chk == 0xfe)
            {
                code_buf[l_codelen++] = 0xfe;
                code_buf[l_codelen++] = 0x00;
            }
            else
                code_buf[l_codelen++] = chk;	//校验码
            code_buf[l_codelen++] = 0xff;		//结束标志

            for (i = 0; i < l_codelen; i++)
            {
                buf[l_netsendlen++] = code_buf[i];
            }
            alen = l_codelen;
        }

        private void DataDecoding(Byte[] buf, ref byte alen)
        {
            byte i;
            byte l_targetlen = 0;
            byte start = 0;
            uint l_index = 0;
            Byte chk = 0;
            Byte[] target_buf = new Byte[1024];

            if (alen <= 0)
            {
                return;
            }

            if (0xff != buf[0])
            {
                return;
            }

            /* ****************解码，去掉起始标志、校验码和结束标志**************** */
            if (0xff == buf[1])
            {
                start = 2;
            }
            else
            {
                start = 1;
            }

            for (i = start; i < alen - 1; i++)
            {
                target_buf[l_targetlen++] = buf[i];
                if (0xfe == buf[i])
                {
                    target_buf[l_targetlen - 1] |= buf[i + 1];
                    i++;
                }
            }

            if (l_targetlen <= 1)
            {
                return;
            }
            else
            {
                for (i = 0; i < l_targetlen - 1; i++)
                {
                    chk ^= target_buf[i];
                }

                if (chk != target_buf[l_targetlen - 1])
                {
                    return;
                }

                alen = (byte)(l_targetlen - 1);
                for (i = 0; i < alen; i++)
                {
                    buf[l_index++] = target_buf[i];
                }
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            carnum = 0;
            g_bSendDataLen = 0;
            TXSendData.Text = "";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string str_tmp = null;
            int l_ntmp = 0;
            int l_cMac = 0;

            int l_ncmd = int.Parse(comboCmd.SelectedIndex.ToString());
            if (l_ncmd == 0)
            {
                MessageBox.Show("基本设置命令，禁止添加车辆！");
                return;
            }

            if (carnum == 0)
            {
                MessageBox.Show("先添加一帧，再添加车辆！");
                return;
            }

            if (carnum >= 4)
            {
                MessageBox.Show("最多允许添加4辆车！");
                return;
            }
            if (timer1.Enabled)
            {
                MessageBox.Show("发送中禁止添加车辆！");
                return;
            }
            carnum++;
            byte[] l_abPackData = new byte[50];
            char[] l_abTmpData = new char[10];
            byte l_bIndex = 0;

            //l_abPackData[l_bIndex++] = 0x01;//CMDType ,1通信命令,0设置命令

            //string str_tmp = TB_485Address.Text.ToString();
            //int l_ntmp = int.Parse(str_tmp);
            //l_abPackData[l_bIndex++] = (byte)(l_ntmp);//485地址

            //l_abPackData[l_bIndex++] = 0x01;//车辆数，先默认为1

            //Byte[] l_au8CBDTime = new Byte[8];
            //if (radioButton1.Checked)
            //{
            //    String l_strSystemTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            //    get_BCDTime(l_au8CBDTime, l_strSystemTime);
            //}
            //else l_au8CBDTime =getTimeSpan(DateTime.Now);

            //for (int n = 0; n < 8; n++)
            //{
            //    l_abPackData[l_bIndex++] = l_au8CBDTime[n];
            //}

            //str_tmp = TB_LaserDealyTime.Text.ToString();
            //l_ntmp = int.Parse(str_tmp);
            //l_abPackData[l_bIndex++] = (byte)l_ntmp;//激光延迟时间，默认40ms

            //            g_abSendData[4] = (byte)carnum;//车辆ID
            //           l_abPackData[l_bIndex++] = (byte)carnum;//车辆ID，默认为0x08

            if (l_ncmd == 2)
            {
                l_abPackData[l_bIndex++] = Convert.ToByte(TB_MAC1.Text, 16);
                l_abPackData[l_bIndex++] = Convert.ToByte(TB_MAC2.Text, 16);
                l_abPackData[l_bIndex++] = Convert.ToByte(TB_MAC3.Text, 16);

                l_cMac = Convert.ToByte(TB_MAC4.Text, 16);
                l_cMac += carnum - 1;

                if (l_cMac > 255)
                {
                    l_cMac -= 256;
                }

                l_abPackData[l_bIndex++] = (byte)(l_cMac);
            }
            else
            {
                str_tmp = textBox1.Text.ToString();
                l_ntmp = int.Parse(str_tmp);

                l_ntmp += carnum - 1;
                if (l_ntmp > 254)
                {
                    l_ntmp -= 254;
                }

                l_abPackData[l_bIndex++] = (byte)(l_ntmp);//车辆ID，默认为0x08
            }

            str_tmp = TB_CMD.Text.ToString();
            l_ntmp = int.Parse(str_tmp);
            l_abPackData[l_bIndex++] = (byte)l_ntmp;//CMD,进行车牌识别，延迟输出识别结果



            str_tmp = TB_cameraDelay.Text.ToString();
            l_ntmp = int.Parse(str_tmp);
            l_abPackData[l_bIndex++] = (byte)(l_ntmp >> 8);//相机识别结果输出延时时间，高八位
            l_abPackData[l_bIndex++] = (byte)(l_ntmp - (byte)(l_ntmp >> 8) * 256);//相机识别结果输出延时时间，低八位



            str_tmp = TB_PixelX11.Text.ToString();
            l_ntmp = int.Parse(str_tmp);
            l_abPackData[l_bIndex++] = (byte)(l_ntmp >> 8);// 左上角X像素值，高八位
            l_abPackData[l_bIndex++] = (byte)(l_ntmp - (byte)(l_ntmp >> 8) * 256);// 左上角X像素值，低八位


            str_tmp = TB_PixelY11.Text.ToString();
            l_ntmp = int.Parse(str_tmp) + (carnum - 1) * 200;
            l_abPackData[l_bIndex++] = (byte)(l_ntmp >> 8);// 左上角Y像素值，高八位
            l_abPackData[l_bIndex++] = (byte)(l_ntmp - (byte)(l_ntmp >> 8) * 256);// 左上角Y像素值，低八位


            str_tmp = TB_PixelX12.Text.ToString();
            l_ntmp = int.Parse(str_tmp);
            l_abPackData[l_bIndex++] = (byte)(l_ntmp >> 8);// 右上角X像素值，高八位
            l_abPackData[l_bIndex++] = (byte)(l_ntmp - (byte)(l_ntmp >> 8) * 256);// 右上角X像素值，低八位


            str_tmp = TB_PixelY12.Text.ToString();
            l_ntmp = int.Parse(str_tmp) + (carnum - 1) * 200;
            l_abPackData[l_bIndex++] = (byte)(l_ntmp >> 8);// 右上角Y像素值，高八位
            l_abPackData[l_bIndex++] = (byte)(l_ntmp - (byte)(l_ntmp >> 8) * 256);// 右上角Y像素值，低八位

            //DataCoding(l_abPackData, ref l_bIndex);
            DataDecoding(g_abSendData, ref g_bSendDataLen);
            g_abSendData[2] = (byte)carnum;

            ///////////////////////////
            //for (int i = 0; i < g_bSendDataLen; i++ )
            //{
            //    g_abSendData[i] = g_abSendData[i + 2];
            //}
            //g_bSendDataLen -= 4;

            for (int i = g_bSendDataLen; i < l_bIndex + g_bSendDataLen; i++)
            {
                g_abSendData[i] = l_abPackData[i - g_bSendDataLen];
            }
            g_bSendDataLen += (byte)(l_bIndex);

            DataCoding(g_abSendData, ref g_bSendDataLen);

            str_tmp = null;
            for (int l_ntmpi = 0; l_ntmpi < g_bSendDataLen; l_ntmpi++)
            {
                str_tmp += g_abSendData[l_ntmpi].ToString("X2");
                str_tmp += " ";
            }
            TXSendData.Text = str_tmp;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (g_bSendDataLen < 28)
            {
                MessageBox.Show("请添加帧！");
                return;
            }
            if (button3.Text == "循环发送")
            {
                timer1.Interval = Convert.ToInt32(comboBox1.Text);
                timer1.Enabled = true;
                button3.Text = "停止发送";
            }
            else
            {
                button3.Text = "循环发送";
                timer1.Enabled = false;
            }

            num = 0;
            index = 1;
            maxnum = Convert.ToInt32(comboBox2.Text);
            label14.Text = "0";
        }

        int num = 0;
        int maxnum = 300;
        int index = 1;
        private void timer1_Tick(object sender, EventArgs e)
        {
            byte l_cOffset = 0;

            DataDecoding(g_abSendData, ref g_bSendDataLen);

            int l_ncmd = int.Parse(comboCmd.SelectedIndex.ToString());
            if (l_ncmd == 2)
            {
                l_cOffset = 3;
            }
            else
            {
                l_cOffset = 0;
            }

            if (num >= maxnum)
            {
                timer1.Enabled = false;
                button3.Text = "循环发送";
                return;
            }

            if (checkBox2.Checked)
            {
                g_abSendData[1] = (byte)(int.Parse(comboAddress.SelectedIndex.ToString()) + num % 2);
            }

            if (index > 254) index = 1;
            g_abSendData[12 + l_cOffset] = (byte)index;


            if (!checkBox1.Checked)
            {
                Byte[] l_au8CBDTime = new Byte[8];
                String l_strSystemTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                get_BCDTime(l_au8CBDTime, l_strSystemTime);
                for (int n = 0; n < 8; n++)
                {
                    g_abSendData[3 + n] = l_au8CBDTime[n];
                }

            }

            DataCoding(g_abSendData, ref g_bSendDataLen);

            //byte XOR = ConBCC(g_abSendData, 0, 26 + l_cOffset);
            //if (XOR == 0xFF)
            //{
            //    g_abSendData[26 + l_cOffset] = 0xFE;
            //    g_abSendData[27 + l_cOffset] = 0x01;
            //    g_abSendData[28 + l_cOffset] = 0xFF;
            //    g_bSendDataLen = 29;
            //    g_bSendDataLen += l_cOffset;
            //}
            //else if (XOR == 0xFE)
            //{
            //    g_abSendData[26 + l_cOffset] = 0xFE;
            //    g_abSendData[27 + l_cOffset] = 0x00;
            //    g_abSendData[28 + l_cOffset] = 0xFF;
            //    g_bSendDataLen = 29;
            //    g_bSendDataLen += l_cOffset;
            //}
            //else
            //{
            //    g_abSendData[26 + l_cOffset] = XOR;
            //    g_abSendData[27 + l_cOffset] = 0xFF;
            //    g_abSendData[28 + l_cOffset] = 0x00;
            //    g_bSendDataLen = 28;
            //    g_bSendDataLen += l_cOffset;
            //}


            //if (index == 254)
            //{
            //    List<byte> tem = g_abSendData.ToList<byte>();
            //    tem.Insert(15, 0);
            //    g_bSendDataLen++;
            //    g_Comm.Write(tem.ToArray(), 0, g_bSendDataLen);

            //    //string str_tmp = "";
            //    //for (int l_ntmpi = 0; l_ntmpi < g_bSendDataLen; l_ntmpi++)
            //    //{
            //    //    str_tmp += tem[l_ntmpi].ToString("X2");
            //    //    str_tmp += " ";
            //    //}
            //    //TXSendData.AppendText("\r\n" + str_tmp);
            //    //Console.WriteLine(str_tmp);
            //}
            //else
            {
                if (g_Comm.IsOpen)
                {
                    g_Comm.Write(g_abSendData, 0, g_bSendDataLen);
                }
                if (g_Comm2.IsOpen)
                {
                    g_Comm2.Write(g_abSendData, 0, g_bSendDataLen);
                }
                //string str_tmp = "";
                //for (int l_ntmpi = 0; l_ntmpi < g_bSendDataLen; l_ntmpi++)
                //{
                //    str_tmp += g_abSendData[l_ntmpi].ToString("X2");
                //    str_tmp += " ";
                //}
                //TXSendData.AppendText("\r\n" + str_tmp);
                //Console.WriteLine(str_tmp);
            }

            index++;
            num++;
            label14.Text = num + "";
        }

        public static byte ConBCC(byte[] temp, int StartIndex, int len)
        {
            byte A = 0;
            for (int i = StartIndex; i < len; i++)
            {
                A ^= temp[i];
            }
            return A;
        }

        public static byte[] getTimeSpan(DateTime dt)
        {
            DateTime dt2 = Convert.ToDateTime("0001-01-01 00:00:00");
            long tem = Convert.ToInt64((dt - dt2).TotalMilliseconds);
            return ToBytes(tem, 8, true);
        }

        public static byte[] ToBytes(long num, int BytesLen, bool highBefore)
        {
            byte[] bt = new byte[BytesLen];
            try
            {
                if (BytesLen == 1)
                    return new byte[] { Convert.ToByte(num) };
                else if (BytesLen == 2)
                    bt = BitConverter.GetBytes((short)num);
                else
                    bt = BitConverter.GetBytes(num);
                if (highBefore) Array.Reverse(bt);
            }
            catch { return null; }
            return bt;
        }

        private void buttonOpenClose2_Click(object sender, EventArgs e)
        {
            try
            {
                if (comboPortName2.Text != "")
                {
                    //根据当前串口对象，来判断操作
                    if (g_Comm2.IsOpen)
                    {
                        //打开时点击，则关闭串口
                        g_Comm2.Close();
                    }
                    else
                    {
                        //关闭时点击，则设置好端口，波特率后打开
                        g_Comm2.PortName = comboPortName2.Text;
                        g_Comm2.BaudRate = int.Parse(comboBaudrate.Text);
                        try
                        {
                            g_Comm2.Open();
                        }
                        catch (Exception ex)
                        {
                            //捕获到异常信息，创建一个新的comm对象，之前的不能用了。
                            g_Comm2 = new SerialPort();
                            //现实异常信息给客户。
                            MessageBox.Show(ex.Message);
                        }
                    }

                }
                //设置按钮的状态
                buttonOpenClose2.Text = g_Comm2.IsOpen ? "关闭串口" : "打开串口";
                BTNSendData.Enabled = g_Comm2.IsOpen;
                button3.Enabled = g_Comm2.IsOpen;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        private void timer2_Tick(object sender, EventArgs e)
        {
            txb_time.Text = DateTime.Now.ToString("hh:mm:ss.fff");
        }
    }
}
