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
    public partial class FormMultiLog : Form
    {
        CheckBox[] checkbox_arr;
        RichTextBox[] rtb_arr;
        TextBox[] txb_arr;
        Color[] color_arr = { Color.Red, Color.DarkOrange, Color.Purple };
        public FormMultiLog()
        {
            InitializeComponent();
            rtb_arr = new RichTextBox[] { richTextBox1, richTextBox2, richTextBox3, richTextBox4 };
            checkbox_arr = new CheckBox[] { checkBox1, checkBox2, checkBox3 };
            txb_arr = new TextBox[] { Txb_SearchName1, Txb_SearchName2, Txb_SearchName3 };
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
                    ForeachFile(str);
                    listBox1.SetSelected(0, true);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
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
                    if (fileName.Contains(".log") || fileName.Contains(".txt"))
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

        private void splitContainer_Resize(object sender, EventArgs e)
        {
            if (splitContainer1.Width != 0)
            {
                splitContainer1.SplitterDistance = splitContainer1.Width / 2;
                splitContainer2.SplitterDistance = splitContainer2.Height / 2;
                splitContainer3.SplitterDistance = splitContainer3.Height / 2;
            }
        }
        private void Btn_Clear_Click(object sender, EventArgs e)
        {
            for (int i=0;i<4;i++)
            {
                if (checkedListBox1.GetItemChecked(i))
                {
                    rtb_arr[i].Clear();
                }
            }
        }


        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                if (listBox1.SelectedItem != null)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (rtb_arr[i].Text == "")
                        {
                            rtb_arr[i].Text = File.ReadAllText(listBox1.SelectedItem.ToString());
                            checkedListBox1.SetItemChecked(i, true);
                            checkedListBox1.Items[i] = Path.GetFileName(listBox1.SelectedItem.ToString());
                            break;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }


        Dictionary<int, Dictionary<RichTextBox, List<int>>> search_dic_total = new Dictionary<int, Dictionary<RichTextBox, List<int>>>();//选中的关键词；RichTextBox框，关键词位置；
        int select_index = 0;//当前选中的关键词0,1,2
        int[] order = new int[3];//3组关键词，序号
        string search_name = "";//搜索关键词
        private void Btn_Search_Click(object sender, EventArgs e)
        {
            try
            {
                search_name = txb_arr[select_index].Text.Trim();
                Dictionary<RichTextBox, List<int>> search_dic = new Dictionary<RichTextBox, List<int>>();
                order[select_index] = 0;
                for (int i = 0; i < 4; i++)
                {
                    if (checkedListBox1.GetItemChecked(i))
                    {
                        List<int> index_list = new List<int>();
                        string str = rtb_arr[i].Text;
                        int last_index = 0;
                        while (true)
                        {
                            int index = Regex.Match(str, search_name, RegexOptions.IgnoreCase).Index;
                            if (index == 0)
                            {
                                break;
                            }
                            str = str.Substring(index + search_name.Length);
                            index_list.Add(index + last_index);
                            last_index = index_list[index_list.Count-1] + search_name.Length;
                        }
                        if (index_list.Count > 0)
                        {
                            search_dic.Add(rtb_arr[i], index_list);
                            ColorShow(search_name, rtb_arr[i], index_list[0], color_arr[select_index]);
                        }
                    }
                }
                if (search_dic.Count>0)
                {
                    if (search_dic_total.ContainsKey(select_index))
                    {
                        search_dic_total[select_index] = search_dic;
                    }
                    else
                    {
                        search_dic_total.Add(select_index, search_dic);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void ColorShow(string targetText, RichTextBox richTextBox,int index,Color color)
        {
            richTextBox.Select(index, targetText.Length);
            richTextBox.SelectionColor = color;
            richTextBox.SelectionFont = new Font(richTextBox.SelectionFont, FontStyle.Bold);
            richTextBox.Focus();
        }
        private void Btn_SearchDown_Click(object sender, EventArgs e)
        {
            order[select_index]++;
            SearchMove();
        }
        private void Btn_SearchUp_Click(object sender, EventArgs e)
        {
            order[select_index]--;
            SearchMove();
        }

        void SearchMove()
        {
            try
            {
                if (!search_dic_total.ContainsKey(select_index))
                {
                    return;
                }
                for (int i = 0; i < 4; i++)
                {
                    if (checkedListBox1.GetItemChecked(i))
                    {
                        if (!search_dic_total[select_index].ContainsKey(rtb_arr[i]))
                        {
                            continue;
                        }
                        int index = 0;
                        int count=search_dic_total[select_index][rtb_arr[i]].Count;
                        if (order[select_index] >= count)
                        {
                            index = order[select_index] % count;
                        }
                        else if (order[select_index] < 0)
                        {
                            index = ((order[select_index]% count)+count)%count;
                        }
                        else
                        {
                            index = order[select_index];
                        }
                        ColorShow(search_name, rtb_arr[i], search_dic_total[select_index][rtb_arr[i]][index], color_arr[select_index]);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if ((sender as CheckBox).Checked == true)
            {
                foreach (Control c in (sender as CheckBox).Parent.Controls)
                {
                    if (c is CheckBox)
                    {
                        if (c != sender)
                        {
                            ((CheckBox)c).Checked = false;
                        }
                    }
                }
            }
            if (checkBox1.Checked)
            {
                select_index = 0;
            }
            else if (checkBox2.Checked)
            {
                select_index = 1;
            }
            else if (checkBox3.Checked)
            {
                select_index = 2;
            }
            else
            {
                MessageBox.Show("未勾选搜索关键词，请重试！");
            }
            order[select_index] = 0;//从头开始
        }

        private void Btn_Color_Click(object sender, EventArgs e)
        {
            try
            {
                if (!search_dic_total.ContainsKey(select_index))
                {
                    return;
                }
                for (int i = 0; i < 4; i++)
                {
                    if (checkedListBox1.GetItemChecked(i))
                    {
                        if (!search_dic_total[select_index].ContainsKey(rtb_arr[i]))
                        {
                            continue;
                        }
                        for (int j = 0; j < search_dic_total[select_index][rtb_arr[i]].Count; j++)
                        {
                            ColorShow(search_name, rtb_arr[i], search_dic_total[select_index][rtb_arr[i]][j], color_arr[select_index]);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
