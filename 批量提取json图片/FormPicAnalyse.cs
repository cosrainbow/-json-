using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 批量提取json图片
{
    public partial class FormPicAnalyse : Form
    {
        public FormPicAnalyse()
        {
            InitializeComponent();
            KeyPreview = true;

            listView1.Clear();
            listView1.AllowColumnReorder = true;//用户可以调整列的位置
            listView1.GridLines = true;//显示行与行之间的分隔线    
            listView1.FullRowSelect = true;//要选择就是一行    
            listView1.View = View.Details;//定义列表显示的方式   
            listView1.Scrollable = true;//需要时候显示滚动条   
            listView1.MultiSelect = false; // 不可以多行选择    
            listView1.HeaderStyle = ColumnHeaderStyle.Clickable;
            listView1.View = View.Details;
            listView1.LabelEdit = true;
            listView1.Columns.Add("name");
            listView1.Columns[0].Width = 160; 
            
            for (int i = 1; i <= 8; i++)
            {
                listView1.Columns.Add(i.ToString());//时间栏
                listView1.Columns[i].Width = -2;
            }
        }
        string[] camara_lines=new string[0];
        string[] laser_lines=new string[0];
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string lasertime = "";
                int camera_index = 0;
                

                string name = listBox1.SelectedItem.ToString();
                DrawPic(name);//显示图片
                string picid = name.Substring(name.LastIndexOf('\\') + 1);
                Txb_PicName.Text = picid;
                picid = picid.Split('_')[0];
              
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < camara_lines.Length; i++)
                {
                    if (camara_lines[i].Contains(picid))
                    {
                        camera_index = i;
                        lasertime = camara_lines[i].Substring(camara_lines[i].IndexOf("TriT") + 5, 12);
                        break;
                    }
                }
                if (camera_index == 0)//当没有响应的激光触发时间时，不显示激光日志
                {
                    richTextBox1.Clear();
                    return;
                }

                for (int i = camera_index - 10; i < camera_index + 30; i++)
                {
                    if (i < 0)
                    {
                        continue;
                    }
                    if (i > camara_lines.Length - 1)
                    {
                        break;
                    }
                    sb.Append(camara_lines[i] + "\n");
                }
                richTextBox1.Text = sb.ToString();
                ColorShow(picid, lasertime);//camera 日志上色


                //查找激光触发时间
                sb.Clear();
                int laser_index = 0;
                for (int i = 0; i < laser_lines.Length; i++)
                {
                    if (laser_lines[i].Contains(lasertime))
                    {
                        laser_index = i;
                        break;
                    }
                }
                if (laser_index == 0)//当没有响应的激光触发时间时，不显示激光日志
                {
                    richTextBox2.Clear();
                    return;
                }
                for (int i = laser_index - 10; i < laser_index + 20; i++)
                {
                    if (i < 0)
                    {
                        continue;
                    }
                    if (i > laser_lines.Length - 1)
                    {
                        break;
                    }
                    if (laser_lines[i].Contains("Info") || laser_lines[i].Contains("vCalDynamicLaserPara"))//只显示Info 和vCalDynamicLaserPara行
                    {
                        sb.Append(laser_lines[i] + "\n");
                    }
                }
                richTextBox2.Text = sb.ToString();

                string tril = "TriL";//Laser日志上色
                for (int i = 0; i < richTextBox2.Text.Length - lasertime.Length; i++)
                {
                    if (richTextBox2.Text.Substring(i, lasertime.Length) == lasertime)
                    {
                        richTextBox2.Select(i, lasertime.Length);
                        richTextBox2.SelectionColor = Color.Red;
                        richTextBox2.SelectionFont = new Font(richTextBox1.SelectionFont, FontStyle.Bold);
                    }
                    if (richTextBox2.Text.Substring(i, tril.Length) == tril)
                    {
                        richTextBox2.Select(i, tril.Length + 2);
                        richTextBox2.SelectionFont = new Font(richTextBox1.SelectionFont, FontStyle.Underline);
                        richTextBox2.SelectionColor = Color.OrangeRed;
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        private void ColorShow(string picid, string lasertime)//richTextBox1 picid 上色， Tril
        {
            int index = richTextBox1.Text.IndexOf(picid);
            richTextBox1.Select(index, picid.Length);
            richTextBox1.SelectionColor = Color.Red;
            richTextBox1.SelectionFont = new Font(richTextBox1.SelectionFont, FontStyle.Bold);
            int index2 = richTextBox1.Text.IndexOf(lasertime);
            richTextBox1.Select(index2, lasertime.Length);
            richTextBox1.SelectionColor = Color.Red;
            richTextBox1.SelectionFont = new Font(richTextBox1.SelectionFont, FontStyle.Bold);
        }
        void DrawPic(string name)//Json 提取图片显示在picbox
        {
            string content = File.ReadAllText(name);
            JObject obj = JObject.Parse(content);
            JObject picObj = JObject.Parse(obj["bizContent"].ToString());
            string imageCode = picObj["picInfoList"][0]["image"].ToString();
            byte[] bytes = Convert.FromBase64String(imageCode);
            MemoryStream s = new MemoryStream(bytes, true);
            s.Write(bytes, 0, bytes.Length);
            Image a = new Bitmap(s);
            Bitmap bit = new Bitmap(this.pictureBox1.Width, this.pictureBox1.Height);
            Graphics g = Graphics.FromImage(bit);//从指定的 Image 创建新的 Graphics(绘图)。
            g.DrawImage(a, new Rectangle(0, 0, bit.Width, bit.Height), new Rectangle(0, 0, a.Width, a.Height), GraphicsUnit.Pixel);
            this.pictureBox1.Image = bit;
        }

        public void ForeachFile(string filePathByForeach)//遍历所有图片Json
        {
            DirectoryInfo theFolder = new DirectoryInfo(filePathByForeach);
            DirectoryInfo[] dirInfo = theFolder.GetDirectories();//获取所在目录的文件夹
            FileInfo[] file = theFolder.GetFiles();//获取所在目录的文件
            string fileName = "";
            foreach (FileInfo fileItem in file) //遍历文件
            {
                try
                {
                    fileName = fileItem.Name;
                    if (fileName.Contains("TRC_BVIPU_REQ"))
                    {
                        listBox1.Items.Add(fileItem.FullName);
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            //遍历文件夹
            foreach (DirectoryInfo NextFolder in dirInfo)
            {
                ForeachFile(NextFolder.FullName);
            }
        }
        private void FormPicAnalyse_KeyDown(object sender, KeyEventArgs e)//键盘选项
        {
            switch (e.KeyCode)
            {
                case Keys.D1:
                case Keys.NumPad1:
                    CheckStateChange(0);
                    break;
                case Keys.D2:
                case Keys.NumPad2:
                    CheckStateChange(1);
                    break;
                case Keys.D3:
                case Keys.NumPad3:
                    CheckStateChange(2);
                    break;
                case Keys.D4:
                case Keys.NumPad4:
                    CheckStateChange(3);
                    break;
                case Keys.D5:
                case Keys.NumPad5:
                    CheckStateChange(4);
                    break;
                case Keys.D6:
                case Keys.NumPad6:
                    CheckStateChange(5);
                    break;
                case Keys.D7:
                case Keys.NumPad7:
                    CheckStateChange(6);
                    break;
                case Keys.D8:
                case Keys.NumPad8:
                    CheckStateChange(7);
                    break;
                case Keys.Enter:
                    SubmitResult();
                    break;
                default:
                    break;
            }
        }
        void CheckStateChange(int i)
        {
            bool state = checkedListBox1.GetItemChecked(i);
            checkedListBox1.SetItemChecked(i, !state);
        }//checkbox 状态切换
        void SubmitResult()//提交选择结果
        {
            ListViewItem item = new ListViewItem();
            string name = listBox1.SelectedItem.ToString();
            item.Text = name.Substring(name.Length-22);
            for (int i=0;i<8;i++)
            {
                bool state = checkedListBox1.GetItemChecked(i);
                item.SubItems.Add( Convert.ToInt32(state).ToString());
                checkedListBox1.SetItemChecked(i,false);
            }
            listView1.Items.Add(item);
        }
        private void listView1_MouseUp(object sender, MouseEventArgs e)//listview右键删除
        {
            if (e.Button == MouseButtons.Right)
            {
                listView1.Items.Remove(listView1.FocusedItem);
            }
        }

        private void 打开文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                dialog.RootFolder = Environment.SpecialFolder.Desktop;
                dialog.Description = "请选择文件路径";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string str = dialog.SelectedPath;
                    listBox1.Items.Clear();
                    richTextBox1.Clear();
                    richTextBox2.Clear();

                    ForeachFile(str);

                    DirectoryInfo theFolder = new DirectoryInfo(str).Parent;
                    FileInfo[] files = theFolder.GetFiles();
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (files[i].Name=="Camera.log")
                        {
                            camara_lines = File.ReadAllLines(files[i].FullName);
                        }
                        if (files[i].Name=="Laser.log")
                        {
                            laser_lines = File.ReadAllLines(files[i].FullName);
                        }
                    }
                    listBox1.SetSelected(0, true);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void 保存ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
           SaveFileDialog saveFile = new SaveFileDialog();
           saveFile.Filter = "文件(*.xlsx)|*.xlsx|所有文件|*.*";//指定文件后缀名为Excel 文件。  
            if (saveFile.ShowDialog() == DialogResult.OK)
            {
                string filename = saveFile.FileName;
                if (System.IO.File.Exists(filename))
                {
                    System.IO.File.Delete(filename);//如果文件存在删除文件。  
                }
                DataTable dt = new DataTable();
                dt.Columns.Add(listView1.Columns[0].Text);
                for (int i=0;i<checkedListBox1.Items.Count;i++)
                {
                    dt.Columns.Add(checkedListBox1.Items[i].ToString());
                }
            
            
                DataRow dr;
                int[] sum = new int[8];
                DataRow total_dr=dt.NewRow();
                for (int i = 0; i < listView1.Items.Count; i++)
                {
                    dr = dt.NewRow();
                    for (int j = 0; j < listView1.Columns.Count; j++)
                    {
                        string num_str = listView1.Items[i].SubItems[j].Text.Trim();
                        dr[j] = num_str;
                        if (j>0)
                        {
                            sum[j-1]+=int.Parse(num_str);
                        }
                    }
                    dt.Rows.Add(dr);//每行内容
                }
                total_dr[0] = "总计";
                for (int i = 0; i < sum.Length; i++)
                {
                    total_dr[i + 1] = sum[i].ToString();
                }
                dt.Rows.Add(total_dr);//每行内容
                Form1.DataTable2Excel(new DataTable[] { dt }, filename, new string[] { "一键分析统计结果" });
                //Form1.dataTableToCsvT(dt, filename);
            }
        }

        public static void listViewToDataTable(ListView lv, DataTable dt)
        {
            DataRow dr;
            dt.Clear();
            dt.Columns.Clear();

            for (int k = 0; k < lv.Columns.Count; k++)
            {
                dt.Columns.Add(lv.Columns[k].Text.Trim().ToString());//生成DataTable列头
            }

            for (int i = 0; i < lv.Items.Count; i++)
            {
                dr = dt.NewRow();
                for (int j = 0; j < lv.Columns.Count; j++)
                {
                    dr[j] = lv.Items[i].SubItems[j].Text.Trim();
                }
                dt.Rows.Add(dr);//每行内容
            }
        }

        private void richTextBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button==MouseButtons.Left)
            {
                string targetText = richTextBox1.SelectedText;
                if (targetText.Length==0)
                {
                    return;
                }
                string str = richTextBox1.Text;
                List<int> index_list = new List<int>();
                int last_index = 0;
                while (true)
                {
                    int index = Regex.Match(str, targetText, RegexOptions.IgnoreCase).Index;
                    if (index == 0)
                    {
                        break;
                    }
                    str = str.Substring(index + targetText.Length);
                    index_list.Add(index + last_index);
                    last_index = index_list[index_list.Count - 1] + targetText.Length;
                }
                for (int i = 0; i < index_list.Count; i++)
                {
                    richTextBox1.Select(index_list[i], targetText.Length);
                    richTextBox1.SelectionColor = Color.DarkBlue;
                    richTextBox1.SelectionFont = new Font(richTextBox1.SelectionFont, FontStyle.Bold);
                }
            }
        }
    }
}
