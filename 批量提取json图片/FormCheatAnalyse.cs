using IcyTools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 批量提取json图片
{
    public partial class FormCheatAnalyse : Form
    {
        public FormCheatAnalyse()
        {
            InitializeComponent();
        }
        private void Btn_CheatAnalyse_Click(object sender, EventArgs e)
        {
            try
            {
                bool connStatus = DBService.testCon("MYSQL", "127.0.0.1", "root", "12345", "ElectTollData", 5);
                //bool connStatus = DBService.testCon("MYSQL", "10.100.8.199", "root", "12345", "ElectTollData", 5);
                if (!connStatus)
                {
                    MessageBox.Show("数据库连接失败！");
                    return;
                }
                Btn_CheatAnalyse.Enabled = false;
                string start = dateTimePicker1.Text;
                string end = dateTimePicker2.Text;
                DataTable[] dt = new DataTable[2];

                DBService.Execute("ALTER TABLE `tolldata_cheat` ADD INDEX PicPlate ( `PicPlate` );");

                string sql = string.Format(@"SELECT DetectTime,OBUID,OBUStatus,PicPlate,OBUPlate,ICPlate FROM `tolldata_cheat` 
                                    WHERE PicPlate in
                                    (SELECT DISTINCT PicPlate FROM `tolldata_cheat`  
                                    WHERE   
                                    RepeatRecordType = 0 AND UneffectType = 0
                                    AND OBUID='00000000'   
                                    AND PicPlate!='' AND PicPlate!='默A00000'    
                                    AND DetectTime BETWEEN '{0}' AND '{1}')
                                    ORDER BY PicPlate", start, end);
                dt[0] = IcyDB.QueryTable(sql);

                //查找作弊超过2次的车辆
                sql = string.Format(@"SELECT PicPlate,COUNT(*) FROM tolldata_cheat                                     WHERE PicPlate in                                    (SELECT DISTINCT PicPlate FROM `tolldata_cheat`                                      WHERE                                       RepeatRecordType = 0 AND UneffectType = 0                                    AND OBUID='00000000'                                       AND PicPlate!='' AND PicPlate!='默A00000'                                        AND DetectTime BETWEEN '{0}' AND '{1}')                                    AND                                     (RepeatRecordType = 0 AND UneffectType = 0                                    AND OBUID='00000000'                                       AND PicPlate!='' AND PicPlate!='默A00000')                                    GROUP BY PicPlate HAVING COUNT(*)>2                                    ORDER BY PicPlate", start, end);
                DataTable dt_cheat = IcyDB.QueryTable(sql);

                //对应总表数量
                sql = string.Format(@"SELECT PicPlate,COUNT(*) FROM tolldata_cheat
                                    WHERE PicPlate in
                                    (SELECT PicPlate FROM tolldata_cheat 
                                    WHERE PicPlate in
                                    (SELECT DISTINCT PicPlate FROM `tolldata_cheat`  
                                    WHERE   
                                    RepeatRecordType = 0 AND UneffectType = 0
                                    AND OBUID='00000000'   
                                    AND PicPlate!='' AND PicPlate!='默A00000'    
                                    AND DetectTime BETWEEN '{0}' AND '{1}')
                                    AND 
                                    (RepeatRecordType = 0 AND UneffectType = 0
                                    AND OBUID='00000000'   
                                    AND PicPlate!='' AND PicPlate!='默A00000')
                                    GROUP BY PicPlate HAVING COUNT(*)>2
                                    ORDER BY PicPlate)
                                    GROUP BY PicPlate", start, end);
                DataTable dt_total = IcyDB.QueryTable(sql);

                dt[1] = new DataTable();
                dt[1].Columns.Add("相机牌识");
                dt[1].Columns.Add("历史疑似作弊次数");
                dt[1].Columns.Add("月总共通行次数");
                dt[1].Columns.Add("疑似作弊比例");
                for (int i = 0; i < dt_total.Rows.Count; i++)
                {
                    int cheat = Convert.ToInt32(dt_cheat.Rows[i]["COUNT(*)"]);
                    int totol = Convert.ToInt32(dt_total.Rows[i]["COUNT(*)"]);
                    double percent=cheat*1.0 / totol;
                    if (percent >= 0.6)
                    {
                        DataRow dr = dt[1].NewRow();
                        dr[0] = dt_total.Rows[i]["PicPlate"];
                        dr[1] = cheat;
                        dr[2] = totol;
                        dr[3] = percent.ToString("P");

                        dt[1].Rows.Add(dr);
                    }
                }
                string filename =  System.Environment.CurrentDirectory + "/log/疑似作弊" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")+".xlsx";
                Log.WriteLog("疑似作弊", "", filename);
                Form1.DataTable2Excel(dt, filename, new string[] { "总表", "疑似作弊情况" });
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            Btn_CheatAnalyse.Enabled = true;
        }
    }
}
