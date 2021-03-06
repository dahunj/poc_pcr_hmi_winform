﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using ABI_POC_PCR.SerialComm;
using System.Threading;

using System.Threading.Tasks;
using ScottPlot;
using ABI_POC_PCR.GraphPlot;
using System.ComponentModel;
using System.Diagnostics;
using System.Data;
using System.Net.Json;

//public class ProgressBarEx : ProgressBar { 
//    private SolidBrush brush = null; 
//    public ProgressBarEx() { 
//        this.SetStyle(ControlStyles.UserPaint, true); 
//    } 
//    protected override void OnPaint(PaintEventArgs e) { 
//        if (brush == null || brush.Color != this.ForeColor) 
//            brush = new SolidBrush(this.ForeColor); 
//        Rectangle rec = new Rectangle(0, 0, this.Width, this.Height); 

//        if (ProgressBarRenderer.IsSupported) 
//            ProgressBarRenderer.DrawHorizontalBar(e.Graphics, rec); 
//        rec.Width = (int)(rec.Width * ((double)Value / Maximum)) - 4; 
//        rec.Height = rec.Height - 4; 
//        e.Graphics.FillRectangle(brush, 2, 2, rec.Width, rec.Height); 
//    } 
//}

namespace ABI_POC_PCR
{
    public partial class MainFrm : Form
    {
        string[] allDataArray;

        Random r = new Random();

        MotionManager mm = new MotionManager();

        SharedMemory sm = SharedMemory.GetInstance();

        BarcodeProtocol serialBarcode = BarcodeProtocol.GetInstance();

        public DataGridView selectedDgv;

        DateTime testStartTime;
        DateTime testEndTime;

        string test_start_time;
        string test_end_time;
        ListViewItem lvi1;
        ListViewItem lvi3;
        ListViewItem lvi4;
        ListViewItem lvi5;

        #region[[[[[[[[[기존 변수]]]]]]]]]
        int userMode = 0;
        int processStep = 0;
        int receiveDataStep = 0;                // BASE, 15, 20, 25, ... FiNAL = 1, ..., 8  // Base 측정값이 오면 1, Final 측정값이 오면 8
        int temp_Idx = 0;

        int iRoutine_cnt = 0;
        int iTube_no = 0;
        int iDye = 0;
        double[] CtlineVal = new double[Plotter.DYE_CNT * Plotter.CH_CNT];
        double[] BaselineVal = new double[Plotter.DYE_CNT * Plotter.CH_CNT];
        double[] BaseStandardDeviation = new double[Plotter.DYE_CNT * Plotter.CH_CNT];
        double[] zerosetValArray = new double[Plotter.DYE_CNT * Plotter.CH_CNT];



        //int[] iRoutine_cnt = new int[COL_CNT * CH_CNT * OPT_VAL_CNT];
        //int[] iTube_no = new int[COL_CNT * CH_CNT * OPT_VAL_CNT];
        //int[] iDye = new int[COL_CNT * CH_CNT * OPT_VAL_CNT];

        static bool[] Optic_Measure_Index_Flag = new bool[60]
        {
            //0,    1,     2,     3,      4,      5,       6,       7,      8,       9 
            false, false, false, false, false, false, false, false,  false, false, //9
            true, false, false, false, false, true,  true,  true,  true,   true, //19
            true, true, true, true, true, true, true, true, true, true, //29
            true, true, true, true, true, true, true, true, true, true, //39
            true, false, false, false, false, true, false, false, false, false, //49
            false, false, false, false, false, false, false, false, false, false//59
        };
        /*
        static int[] Optic_Measure_Idx = new int[60]
        {
           //0,  1,   2,   3,   4,   5,   6,   7,   8,   9 
            0,   0,   0,   0,   0,   0,   0,   1,   0,   0, //9
            0,   0,   0,   0,   0,   2,   3,   4,   5,   6, //19
            7,   8,   9,  10,  11,  12,  13,  14,  15,  16, //29
           17,  18,  19,  20,  21,  22,  23,  24,  25,  26, //39
           27,   0,   0,   0,   0,  28,   0,   0,   0,   0, //49
            0,   0,   0,   0,   0,   0,   0,   0,   0,   0 //59
        };
        */

        PcrProtocol serial = PcrProtocol.GetInstance();
        InfoDialog aboutBox;

        int selectedRowCount = 0;
        private object tb_deID_IDManage;
        public Dictionary<String, String> cmd_dic;


        bool isConnected = false;
        int nProgressBar = 0;

        bool bLoadedRecipe = false; // 레시피를 읽었는가?

        // 로그 파일 생성용
        LogWriter logToFile = new LogWriter();
        LogWriter logForMonitor = new LogWriter();

        bool bSaveLog = true;

        string[] headerName = { "", "", "", "" };
        string[] chamberName = { "", "", "", "" };
        string strData;
        string[,] ALL_DATA;
        string[,] MEASURED_DATA;
        string[,] DISPLAY_DATA;
        string[,] final_data;
        string[] CH_BASE_DATA;

        double[] scaleFactor = new double[Plotter.CH_CNT * Plotter.DYE_CNT];

        bool firstRecv = false;
        int recv_cnt = 0;
        int routine_cnt = -1;

        // 데이터 버퍼 업데이트 확인용;
        bool bUpdated = false;

        // 시작여부 확인용
        bool b_start_Process = false;
        bool b_check_Door = false;
        static int nTimerNo = 0;


        bool globRecv = false;
        bool b_GUI_Init = false;

        //delegate void GetSerialStringCallback(object obj);
        string waitData;
        string[] procData;

        string allData;
        delegate void SetDataBoxCallback(string str);
        delegate void SetTextBoxCallback(TextBox tb, string str);

        /// <summary>
        /// 0907
        /// 데이터 수신 관련 기존 소스에서 추가
        /// </summary>
        string data_tb;
        public string Data_tb
        {
            get
            {
                return data_tb;
            }
            set
            {
                if (data_tb != value)
                {
                    data_tb = value;
                    //NotifyPropertyChanged("Data_tb");
                }
            }
        }
        #endregion

        ////////////////////////////////////////////

        public MainFrm()
        {
            InitializeComponent();
            
            //프로그램 실행시 Data 폴더 확인 및 없을경우 Data 폴더 생성
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\data");
            if (!di.Exists) { di.Create(); }
            System.IO.DirectoryInfo di2 = new System.IO.DirectoryInfo(Application.StartupPath + @"\log");
            if (!di2.Exists) { di2.Create(); }
            System.IO.DirectoryInfo di3 = new System.IO.DirectoryInfo(Application.StartupPath + @"\report");
            if (!di3.Exists) { di3.Create(); }

            // 초기 컴포넌트 세팅

            // 그리드 해더 이름 읽어와서 설정
            setHeaderName();

            // 그리드 기본 값 설정
            //presetDataGrid_Tester(dataGridView1_Tester, chamberName[0], "", "", "", "");
            //presetDataGrid_Tester(dataGridView2_Tester, chamberName[1], "", "", "", "");
            //presetDataGrid_Tester(dataGridView3_Tester, chamberName[2], "", "", "", "");
            //presetDataGrid_Tester(dataGridView4_Tester, chamberName[3], "", "", "", "");

            presetDataGrid_Tester(dgv_ct1, chamberName[0], "2100", "1500", "500", "500");
            presetDataGrid_Tester(dgv_ct2, chamberName[1], "500", "2000", "1500", "1500");
            presetDataGrid_Tester(dgv_ct3, chamberName[2], "2100", "500", "2500", "1500");
            presetDataGrid_Tester(dgv_ct4, chamberName[3], "1500", "500", "2500", "1500");


            //presetDataGrid_Tester(dataGridView2_Tester, chamberName[1]);
            //presetDataGrid_Tester(dataGridView3_Tester, chamberName[2]);
            //presetDataGrid_Tester(dataGridView4_Tester, chamberName[3]);

            presetDataGrid_Engineer(dataGridView1_Engineer, chamberName[0]);
            presetDataGrid_Engineer(dataGridView2_Engineer, chamberName[1]);
            presetDataGrid_Engineer(dataGridView3_Engineer, chamberName[2]);
            presetDataGrid_Engineer(dataGridView4_Engineer, chamberName[3]);

            //presetDataGrid_ct(dataGridView5, chamberName[0]);
            //presetDataGrid_ct(dataGridView6, chamberName[1]);
            //presetDataGrid_ct(dataGridView7, chamberName[2]);
            //presetDataGrid_ct(dataGridView8, chamberName[3]);

            // 진행바 칼라 설정
            progressBar_step.ForeColor = Color.FromArgb(255, 0, 0);
            progressBar_step.BackColor = Color.FromArgb(150, 0, 0);

            // 작업진행상황 초기화
            setProcessMode(0);

            // 유저모드 초기화
            setUserMode(0);

            //////////////////////////////////////////////////////////////////////////////////
            ///// 데이터그리드 업데이트 테스트
            ///
            /*
            // 데이터 그리드 각 열에 대하여 데이터 4개를 변경할 수 있음
            // 전달인자 : 1, 1, 4개 값 --> 챔버1, 1번열, 4개의 값
            setDataGrid_Tester(dataGridView1_Tester, 1, "1", "2", "3", "4");
            setDataGrid_Tester(dataGridView2_Tester, 2, "11", "22", "33", "44");
            setDataGrid_Tester(dataGridView3_Tester, 3, "111", "222", "333", "444");
            setDataGrid_Tester(dataGridView4_Tester, 4, "1111", "2222", "3333", "4444");

            // 데이터 그리드 각 열에 대하여 데이터 4개를 변경할 수 있음
            // 전달인자 : 1, 1, 4개 값 --> 챔버1, 1번열, 4개의 값
            setDataGrid_Engineer(dataGridView1_Engineer, 1, 1, "a1", "a2", "a3", "a4"); 
            setDataGrid_Engineer(dataGridView1_Engineer, 1, 2, "b1", "b2", "b3", "b4");
            setDataGrid_Engineer(dataGridView1_Engineer, 1, 3, "c1", "c2", "c3", "c4");
            setDataGrid_Engineer(dataGridView1_Engineer, 1, 4, "d1", "d2", "d3", "d4");
            setDataGrid_Engineer(dataGridView1_Engineer, 1, 5, "e1", "e2", "e3", "e4");
            setDataGrid_Engineer(dataGridView1_Engineer, 1, 6, "f1", "f2", "f3", "f4");
            setDataGrid_Engineer(dataGridView1_Engineer, 1, 7, "g1", "g2", "g3", "g4");
            setDataGrid_Engineer(dataGridView1_Engineer, 1, 8, "h1", "h2", "h3", "h4");

            // 데이터 그리드 각 열에 대하여 데이터 4개를 변경할 수 있음
            // 전달인자 : 1, 1, 4개 값 --> 챔버1, 1번열, 4개의 값
            setDataGrid_Engineer(dataGridView2_Engineer, 1, 1, "a1", "a2", "a3", "a4");
            setDataGrid_Engineer(dataGridView2_Engineer, 1, 2, "b1", "b2", "b3", "b4");
            setDataGrid_Engineer(dataGridView3_Engineer, 1, 3, "c1", "c2", "c3", "c4");
            setDataGrid_Engineer(dataGridView3_Engineer, 1, 4, "d1", "d2", "d3", "d4");
            setDataGrid_Engineer(dataGridView4_Engineer, 1, 5, "e1", "e2", "e3", "e4");
            setDataGrid_Engineer(dataGridView4_Engineer, 1, 6, "f1", "f2", "f3", "f4");
            setDataGrid_Engineer(dataGridView2_Engineer, 1, 7, "g1", "g2", "g3", "g4");
            setDataGrid_Engineer(dataGridView3_Engineer, 1, 8, "h1", "h2", "h3", "h4");
            */

            cmd_dic = new Dictionary<string, string>();

            cmd_dic.Add(":ROUTINE_CYCLE_MAX", "");
            cmd_dic.Add(":OPTIC_OPERATION_KEEPING_TEMP_SEC", "");
            cmd_dic.Add(":OPTIC_NO_OPERATION_KEEPING_TEMP_SEC", "");
            cmd_dic.Add(":DELAY_TIME_BEFORE_OPTING_RUNING", "");
            cmd_dic.Add(":KEEPING_MINUTE_PELTIER_TEMPERATURE", "");
            cmd_dic.Add(":KEEPING_TIME_FOR_HIGH_TEMPERATURE", "");
            cmd_dic.Add(":HEAT_SETPOINT", "");
            cmd_dic.Add(":COOL_SETPOINT", "");

            cmd_dic.Add(":PRE_COND_SETPOINT", "");
            cmd_dic.Add(":PRECOND_KEEPING_TIME_MIN", "");

            cmd_dic.Add(":RT_PRE_COND_SETPOINT", "");
            cmd_dic.Add(":RT_PRECOND_KEEPING_TIME_MIN", "");

            cmd_dic.Add(":OPTIC_CURRENT_FAM", "");
            cmd_dic.Add(":OPTIC_CURRENT_ROX", "");
            cmd_dic.Add(":OPTIC_CURRENT_HEX", "");
            cmd_dic.Add(":OPTIC_CURRENT_CY5", "");

            cmd_dic.Add(":MACHINE_NUM", "");


            
            serial.ReceivedEvent += GetSerialString;
            serial.ReceivedPacketEvent += RxPacket_CallBack;

            /// 그리드 배열에 넣은 버퍼 생성  
            ALL_DATA = new string[Plotter.CH_CNT * 4, Plotter.CYCLE_CNT];  // 16 by 7  --> cycle
            MEASURED_DATA = new string[Plotter.CH_CNT * Plotter.DYE_CNT, Plotter.COL_CNT];
            DISPLAY_DATA = new string[Plotter.CH_CNT * Plotter.DYE_CNT, Plotter.COL_CNT];
            final_data = new string[Plotter.CH_CNT * Plotter.DYE_CNT, Plotter.COL_CNT];
            CH_BASE_DATA = new string[Plotter.CH_CNT * Plotter.DYE_CNT];     // 16      --> Base

            ///////////////////////////////////////////////////
            /// 변수들 초기화

            _reset_Process();

            /////////////////////////////////////////////////////
            ///// 그리드 배열에 넣은 값 초기화
            //ALL_DATA = new string[CH_CNT * 4, COL_CNT];  // 16 by 7 ???
            //CH_BASE_DATA = new string[CH_CNT * 4];     // 16

            //for (int i = 0; i < CH_CNT * 4; i++)
            //{
            //    CH_BASE_DATA[i] = "";
            //    for(int j = 0; j < COL_CNT; j++)
            //    {
            //        ALL_DATA[i, j] = "";
            //    }
            //}

            // pb_Logo.Image = System.Drawing.Image.FromFile("data\\logo.bmp");

            Plotter.Init();

            //tb_ID_Main.Text = "ABI2";
            //tb_PW_Main.Text = "5344";
            //SetScottPlot();

            serialBarcode.ReceivedEvent += GetSerialStringBarcode;

            record_load();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //OnClickNewDGVAnalyticResult(sender, e);//createAnalyticDGV();
            

            init_Test_Result();

            ctLimit_Load();
           
            updateDeviceStatus("Stop");

            sm.testName = "COVID";
            chkbox_use_standard_dev.Checked = true;
            chkBox_BaselineScale.Checked = true;

            drawDataGridView(dgvCtTable1, 0, 0, 0, 0);
            drawDataGridView(dgvCtTable2, 0, 0, 0, 0);
            drawDataGridView(dgvCtTable3, 0, 0, 0, 0);
            drawDataGridView(dgvCtTable4, 0, 0, 0, 0);

            loadCsv_BaseCycles(dgv_CycleForBaseCalculation);

            dgv_CycleForBaseCalculation.Rows.Add();

            dgv_opticDatum_tube1.Location = new Point(33, 60);
            dgv_opticDatum_tube2.Location = new Point(33, 230);
            dgv_opticDatum_tube3.Location = new Point(33, 400);
            dgv_opticDatum_tube4.Location = new Point(33, 570);

            lb_opticMeasurementCycleSelection.Visible = false;
            dgv_opticMeasurementCycleSelection.Visible = false;

            gb_CartridgeInfo.Enabled = false;
            gb_TestInfo.Enabled = false;

            setAccessibility();

            cb_Test_Interpretation.SelectedIndex = 0;

            dgv_interpretation_howTo.Visible = true;
        

            initDiagnosisCT();
            initBaseValue();
            
            //initDgvDiagnosis();

            sm.opticReceivedFlag = false;
            if (!backgroundWorker1.IsBusy)
            {
                _inputparameter.Delay = 0;

                backgroundWorker1.RunWorkerAsync(_inputparameter);
            }

            this.WindowState = FormWindowState.Maximized;

            // combo_comList = 콤보 박스
            //COM Port 리스트 얻어 오기
            string[] comlist = System.IO.Ports.SerialPort.GetPortNames();

            //COM Port가 있는 경우에만 콤보 박스에 추가.
            if (comlist.Length > 0)
            {
                cb_Port_Main.Items.AddRange(comlist);
                //제일 처음에 위치한 녀석을 선택
                cb_Port_Main.SelectedIndex = 0;
            }

            if (comlist.Length > 0)
            {
                cb_Port_Barcode.Items.AddRange(comlist);
                //제일 처음에 위치한 녀석을 선택
                cb_Port_Barcode.SelectedIndex = 0;
            }

            aboutBox = new InfoDialog();

            //제일 처음에 위치한 녀석을 선택
            tb_Right_IDManage.SelectedIndex = 0;

            // 레시피 파일을 검색해서 리스트를 채움
            SetRecipeCombobox_Test();

            // 라벨 투명하게 설정
            //lb_Setting.BackColor = Color.Transparent;
            //lb_Setting.Parent = progressBar_step;


            // 계정정보 읽기
            btn_Search_IDManage_Click(this, null);

            //lb_IC_Result.Text = sm.IC_Result;

            tabPage_tester_Click();
            // visible false  tabpage 
            //tabControl1.TabPages.Remove(tp_researcher);
            //tabControl_Tester.TabPages.Remove(tabPage1);
            //tabControl_Tester.TabPages.Remove(tabPage9);
            tabControl_Tester.TabPages.Remove(tabPage_rawData);

            tabControl_admin.TabPages.Remove(tp_Table);
            tabControl_admin.TabPages.Remove(tp_Result);

            //tabControl_Engineer.TabPages.Remove(tp_simulation);

            tabControl_Engineer.TabPages.Remove(tp_base_UserSelection);
            //tabControl_Engineer.TabPages.Remove(tp_pcr_options);
            //dataGridView1.AutoGenerateColumns = false;

            //TestResult menu Info. initialize 

            //setDiagnosisTable();

            initTestInfo();
            initTesterInfo();
            initCartridgeInfo();
           
            initializeAnalyticResult();

        }

        private async void SetScottPlot()
        {
            var inputDataY = new double[] { 1, 4, 9, 16, 25, 19, 22, 30, 1, 4, 9, 16, 25, 19, 22, 30, 300, 600, 900, 1200, 1500, 1800, 2100, 2200, 2250, 2220, 2210, 2200 };
            var inputDataY2 = new double[] { 1, 2, 3, 5, 8, 29, 12, 20, 1, 2, 3, 5, 8, 29, 12, 20, 200, 400, 600, 800, 1000, 1200, 1500, 1700, 1750, 1720, 1710, 1800 };
            var inputDataY3 = new double[] { 2, 2, 4, 7, 8, 29, 22, 20, 2, 2, 4, 7, 8, 29, 22, 20, 200, 600, 600, 1200, 1000, 1800, 1500, 2200, 1750, 2220, 2210, 2200 };
            var inputDataY4 = new double[] { 6, 3, 3, 7, 8, 20, 12, 24, 6, 3, 3, 7, 8, 20, 12, 24, 300, 600, 600, 1200, 1500, 1200, 2100, 1700, 2250, 1720, 2210, 2200 };

            var progress = new Progress<int>(index =>
            {
                Plotter.ch1DataDic["FAM"].Add(inputDataY[index]);
                Plotter.ch1DataDic["ROX"].Add(inputDataY2[index]);
                Plotter.ch1DataDic["HEX"].Add(inputDataY3[index]);
                Plotter.ch1DataDic["CY5"].Add(inputDataY4[index]);

                Plotter.ch2DataDic["FAM"].Add(inputDataY[index]);
                Plotter.ch2DataDic["ROX"].Add(inputDataY2[index]);
                Plotter.ch2DataDic["HEX"].Add(inputDataY3[index]);
                Plotter.ch2DataDic["CY5"].Add(inputDataY4[index]);

                Plotter.ch3DataDic["FAM"].Add(inputDataY[index]);
                Plotter.ch3DataDic["ROX"].Add(inputDataY2[index]);
                Plotter.ch3DataDic["HEX"].Add(inputDataY3[index]);
                Plotter.ch3DataDic["CY5"].Add(inputDataY4[index]);

                Plotter.ch4DataDic["FAM"].Add(inputDataY[index]);
                Plotter.ch4DataDic["ROX"].Add(inputDataY2[index]);
                Plotter.ch4DataDic["HEX"].Add(inputDataY3[index]);
                Plotter.ch4DataDic["CY5"].Add(inputDataY4[index]);

                Plotter.UpdatePlot(formsPlot1, "Tube1", 0, Plotter.ch1DataDic, cBoxCh1FAM, cBoxCh1ROX, cBoxCh1HEX, cBoxCh1CY5, 0, 0, 0, 0);
                Plotter.UpdatePlot(formsPlot2, "Tube2", 1, Plotter.ch2DataDic, cBoxCh2FAM, cBoxCh2ROX, cBoxCh2HEX, cBoxCh2CY5, 0, 0, 0, 0);
                Plotter.UpdatePlot(formsPlot3, "Tube3", 2, Plotter.ch3DataDic, cBoxCh3FAM, cBoxCh3ROX, cBoxCh3HEX, cBoxCh3CY5, 0, 0, 0, 0);
                Plotter.UpdatePlot(formsPlot4, "Tube4", 3, Plotter.ch4DataDic, cBoxCh4FAM, cBoxCh4ROX, cBoxCh4HEX, cBoxCh4CY5, 0, 0, 0, 0);
            });

            var plotTask = Task.Run(() => AsyncInputSimulator(progress));
            await plotTask;
        }
        private void AsyncInputSimulator(IProgress<int> progress)
        {
            for (int i = 0; i < 28; i++)
            {
                if (progress != null)
                    progress.Report(i);

                Thread.Sleep(1000);
            }
        }
        private void formsPlot1_MouseMoved(object sender, MouseEventArgs e)
        {
            //사용안함
            //if(ch1FAMHL != null)
            //{
            //    int pixelX = e.X;
            //    int pixelY = e.Y;

            //    double mouseX = formsPlot1.plt.CoordinateFromPixelX(e.Location.X);
            //    double mouseY = formsPlot1.plt.CoordinateFromPixelY(e.Location.Y);

            //    ch1FAMHL.HighlightClear();
            //    var (x, y, index) = ch1FAMHL.HighlightPointNearest(mouseX, mouseY);

            //    if(cBoxCh1FAM.Checked)
            //        formsPlot1.Render(skipIfCurrentlyRendering: true);

            //    PointF highlightedPoint = formsPlot1.plt.CoordinateToPixel(x, y);
            //    double dX = e.Location.X - highlightedPoint.X;
            //    double dY = e.Location.Y - highlightedPoint.Y;
            //    double distanceToPoint = Math.Sqrt(dX * dX + dY * dY);

            //    if (distanceToPoint < 10)
            //        tooltip.Show($"{x}, {y}", this,
            //            (int)highlightedPoint.X + formsPlot1.Location.X,
            //            (int)highlightedPoint.Y + formsPlot1.Location.Y + tabControl4.Location.Y);
            //    else
            //        tooltip.Hide(this);
            //}


            //(double coordinateX, double coordinateY) = formsPlot1.GetMouseCoordinates();

            //Console.WriteLine($"{e.X:0.000}");
            //XPixelLabel.Text = $"{e.X:0.000}";
            //YPixelLabel.Text = $"{e.X:0.000}";
            //Console.WriteLine($"{coordinateX:0.00000000}");

            //XCoordinateLabel.Text = $"{coordinateX:0.00000000}";
            //YCoordinateLabel.Text = $"{coordinateY:0.00000000}";

            //vLine.position = coordinateX;
            //hLine.position = coordinateY;

            //formsPlot1.Render(skipIfCurrentlyRendering: true);
        }
        private void cBoxPlot_CheckedChanged(object sender, EventArgs e)
        {
            var getCBoxName = (sender as CheckBox).Name;
            Plotter.CheckBoxChecked(getCBoxName, (sender as CheckBox).Checked, formsPlot1, formsPlot2, formsPlot3, formsPlot4);
        }
        private void btnRSTPlot1_Click(object sender, EventArgs e)
        {
            Plotter.ViewInit(formsPlot1);
        }
        private void btnRSTPlot2_Click(object sender, EventArgs e)
        {
            Plotter.ViewInit(formsPlot2);
        }
        private void btnRSTPlot3_Click(object sender, EventArgs e)
        {
            Plotter.ViewInit(formsPlot3);
        }
        private void btnRSTPlot4_Click(object sender, EventArgs e)
        {
            Plotter.ViewInit(formsPlot4);
        }

        #region[[[[[[[[[[기존코드]]]]]]]]]]
        private void setDataGrid_Engineer(DataGridView dgv, int chamberIndex, int columindex, string data1, string data2, string data3, string data4)
        {
            // chamberIndex : 1~4
            // columindex : base~final (1~8)

            // 기존 그리드 값을 읽어온 후 값을 업데이트 하고 다시 그리드 값을 설정하자
            //string[] chamberName = { "chamber1", "chamber2", "chamber3", "chamber4" };

            string[] d1 = new string[10] { chamberName[chamberIndex - 1], headerName[0], "0", "0", "0", "0", "0", "0", "0", "0" };
            string[] d2 = new string[10] { chamberName[chamberIndex - 1], headerName[1], "0", "0", "0", "0", "0", "0", "0", "0" };
            string[] d3 = new string[10] { chamberName[chamberIndex - 1], headerName[2], "0", "0", "0", "0", "0", "0", "0", "0" };
            string[] d4 = new string[10] { chamberName[chamberIndex - 1], headerName[3], "0", "0", "0", "0", "0", "0", "0", "0" };

            // 데이터 가져오기
            int rowNo = 0;
            foreach (DataGridViewRow row in dgv.Rows)
            {
                rowNo++;
                if (!row.IsNewRow)
                {
                    for (int i = 0; i < dgv.Columns.Count; i++)
                    {
                        if (rowNo == 1) d1[i] = row.Cells[i].Value.ToString();
                        if (rowNo == 2) d2[i] = row.Cells[i].Value.ToString();
                        if (rowNo == 3) d3[i] = row.Cells[i].Value.ToString();
                        if (rowNo == 4) d4[i] = row.Cells[i].Value.ToString();
                    }
                }
            }

            int toInt;
            float toFloat;

            // 숫자 변환
            float.TryParse(data1, out toFloat); int iD1 = (int)toFloat;
            float.TryParse(data2, out toFloat); int iD2 = (int)toFloat;
            float.TryParse(data3, out toFloat); int iD3 = (int)toFloat;
            float.TryParse(data4, out toFloat); int iD4 = (int)toFloat;

            // 데이터 업데이트
            d1[columindex + 1] = iD1.ToString();
            d2[columindex + 1] = iD2.ToString();
            d3[columindex + 1] = iD3.ToString();
            d4[columindex + 1] = iD4.ToString();

            // 새로운 데이터는 추가
            dgv.Rows.Add(d1);
            dgv.Rows.Add(d2);
            dgv.Rows.Add(d3);
            dgv.Rows.Add(d4);

            // 기존 데이터는 삭제
            dgv.Rows.RemoveAt(0);
            dgv.Rows.RemoveAt(0);
            dgv.Rows.RemoveAt(0);
            dgv.Rows.RemoveAt(0);

            // 그리드뷰를 설정
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (i % 2 != 0)
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.White;
                }
                else
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                }
            }
            /*
            if (Plotter.isValueBase)
            {
                Plotter.ResetAllPlots(formsPlot1, formsPlot2, formsPlot3, formsPlot4);
                Plotter.isValueBase = false;
            }

            switch (chamberIndex)
            {
                case 1:
                    Plotter.ch1DataDic["FAM"].Add(iD1);
                    Plotter.ch1DataDic["ROX"].Add(iD2);
                    Plotter.ch1DataDic["HEX"].Add(iD3);
                    Plotter.ch1DataDic["CY5"].Add(iD4);
                    Plotter.UpdatePlot(formsPlot1, "Chamber1", 0, Plotter.ch1DataDic, cBoxCh1FAM, cBoxCh1ROX, cBoxCh1HEX, cBoxCh1CY5);
                    break;
                case 2:
                    Plotter.ch2DataDic["FAM"].Add(iD1);
                    Plotter.ch2DataDic["ROX"].Add(iD2);
                    Plotter.ch2DataDic["HEX"].Add(iD3);
                    Plotter.ch2DataDic["CY5"].Add(iD4);
                    Plotter.UpdatePlot(formsPlot2, "Chamber2", 1, Plotter.ch2DataDic, cBoxCh2FAM, cBoxCh2ROX, cBoxCh2HEX, cBoxCh2CY5);
                    break;
                case 3:
                    Plotter.ch3DataDic["FAM"].Add(iD1);
                    Plotter.ch3DataDic["ROX"].Add(iD2);
                    Plotter.ch3DataDic["HEX"].Add(iD3);
                    Plotter.ch3DataDic["CY5"].Add(iD4);
                    Plotter.UpdatePlot(formsPlot3, "Chamber3", 2, Plotter.ch3DataDic, cBoxCh3FAM, cBoxCh3ROX, cBoxCh3HEX, cBoxCh3CY5);
                    break;
                case 4:
                    Plotter.ch4DataDic["FAM"].Add(iD1);
                    Plotter.ch4DataDic["ROX"].Add(iD2);
                    Plotter.ch4DataDic["HEX"].Add(iD3);
                    Plotter.ch4DataDic["CY5"].Add(iD4);
                    Plotter.UpdatePlot(formsPlot4, "Chamber4", 3, Plotter.ch4DataDic, cBoxCh4FAM, cBoxCh4ROX, cBoxCh4HEX, cBoxCh4CY5);
                    break;
                default:
                    break;
            }
            */

        }

        private void resetDataGraph_Ct(DataGridView dgv, int chamberIndex, int columindex, string data1, string data2, string data3, string data4, int hValue)
        {
            // 기존 그리드 값을 읽어온 후 값을 업데이트 하고 다시 그리드 값을 설정하자
            //string[] chamberName = { "chamber1", "chamber2", "chamber3", "chamber4" };
            string[] d1 = new string[29] { headerName[0], "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
            string[] d2 = new string[29] { headerName[1], "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
            string[] d3 = new string[29] { headerName[2], "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
            string[] d4 = new string[29] { headerName[3], "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };

            //string[] d1 = new string[10] { chamberName[chamberIndex - 1], headerName[0], "0", "0", "0", "0", "0", "0", "0", "0" };
            //string[] d2 = new string[10] { chamberName[chamberIndex - 1], headerName[1], "0", "0", "0", "0", "0", "0", "0", "0" };
            //string[] d3 = new string[10] { chamberName[chamberIndex - 1], headerName[2], "0", "0", "0", "0", "0", "0", "0", "0" };
            //string[] d4 = new string[10] { chamberName[chamberIndex - 1], headerName[3], "0", "0", "0", "0", "0", "0", "0", "0" };

            // 데이터 가져오기
            int rowNo = 0;
            foreach (DataGridViewRow row in dgv.Rows)
            {
                rowNo++;
                if (!row.IsNewRow)
                {
                    for (int i = 0; i < dgv.Columns.Count; i++)
                    {
                        if (rowNo == 1) d1[i] = row.Cells[i].Value.ToString();
                        if (rowNo == 2) d2[i] = row.Cells[i].Value.ToString();
                        if (rowNo == 3) d3[i] = row.Cells[i].Value.ToString();
                        if (rowNo == 4) d4[i] = row.Cells[i].Value.ToString();
                    }
                }
            }

            int toInt;
            float toFloat;

            // 숫자 변환
            float.TryParse(data1, out toFloat);
            int iD1 = (int)toFloat;

            float.TryParse(data2, out toFloat);
            int iD2 = (int)toFloat;

            float.TryParse(data3, out toFloat);
            int iD3 = (int)toFloat;

            float.TryParse(data4, out toFloat);
            int iD4 = (int)toFloat;

            // 데이터 업데이트
            d1[columindex + 1] = iD1.ToString();
            d2[columindex + 1] = iD2.ToString();
            d3[columindex + 1] = iD3.ToString();
            d4[columindex + 1] = iD4.ToString();



            switch (chamberIndex)
            {
                case 1:
                    Plotter.ch1DataDic["FAM"].RemoveAt(iD1);
                    Plotter.ch1DataDic["ROX"].RemoveAt(iD2);
                    Plotter.ch1DataDic["HEX"].RemoveAt(iD3);
                    Plotter.ch1DataDic["CY5"].RemoveAt(iD4);
                    //Plotter.UpdatePlot(formsPlot1, "Tube1", 0, Plotter.ch1DataDic, cBoxCh1FAM, cBoxCh1ROX, cBoxCh1HEX, cBoxCh1CY5, hValue);
                    break;
                case 2:
                    Plotter.ch2DataDic["FAM"].RemoveAt(iD1);
                    Plotter.ch2DataDic["ROX"].RemoveAt(iD2);
                    Plotter.ch2DataDic["HEX"].RemoveAt(iD3);
                    Plotter.ch2DataDic["CY5"].RemoveAt(iD4);
                    //Plotter.UpdatePlot(formsPlot2, "Tube2", 1, Plotter.ch2DataDic, cBoxCh2FAM, cBoxCh2ROX, cBoxCh2HEX, cBoxCh2CY5, 0);
                    break;
                case 3:
                    Plotter.ch3DataDic["FAM"].RemoveAt(iD1);
                    Plotter.ch3DataDic["ROX"].RemoveAt(iD2);
                    Plotter.ch3DataDic["HEX"].RemoveAt(iD3);
                    Plotter.ch3DataDic["CY5"].RemoveAt(iD4);
                    //Plotter.UpdatePlot(formsPlot3, "Tube3", 2, Plotter.ch3DataDic, cBoxCh3FAM, cBoxCh3ROX, cBoxCh3HEX, cBoxCh3CY5, 0);
                    break;
                case 4:
                    Plotter.ch4DataDic["FAM"].RemoveAt(iD1);
                    Plotter.ch4DataDic["ROX"].RemoveAt(iD2);
                    Plotter.ch4DataDic["HEX"].RemoveAt(iD3);
                    Plotter.ch4DataDic["CY5"].RemoveAt(iD4);
                    //Plotter.UpdatePlot(formsPlot4, "Tube4", 3, Plotter.ch4DataDic, cBoxCh4FAM, cBoxCh4ROX, cBoxCh4HEX, cBoxCh4CY5, 0);
                    break;
                default:
                    break;
            }
        }

        private void updateDataGridOpticDatas(DataGridView[] dgv, string[,] stringArray)
        {
            int iTemp2;

            dgv_opticDatum_tube1.Columns[0].HeaderText = "Dye";
            for (int i = 0; i < Plotter.COL_CNT; i++)
            {
                dgv_opticDatum_tube1.Columns[i + 1].HeaderText = Plotter.Optic_Measure_Idx[i].ToString();
                //dataGridView5.Columns[1].Name = "Threshold";
                //dataGridView5.Columns[2].Name = "Result";
            }

            dgv_opticDatum_tube2.Columns[0].HeaderText = "Dye";
            for (int i = 0; i < Plotter.COL_CNT; i++)
            {
                dgv_opticDatum_tube2.Columns[i + 1].HeaderText = Plotter.Optic_Measure_Idx[i].ToString();
                //dataGridView5.Columns[1].Name = "Threshold";
                //dataGridView5.Columns[2].Name = "Result";
            }

            dgv_opticDatum_tube3.Columns[0].HeaderText = "Dye";
            for (int i = 0; i < Plotter.COL_CNT; i++)
            {
                dgv_opticDatum_tube3.Columns[i + 1].HeaderText = Plotter.Optic_Measure_Idx[i].ToString();
                //dataGridView5.Columns[1].Name = "Threshold";
                //dataGridView5.Columns[2].Name = "Result";
            }

            dgv_opticDatum_tube4.Columns[0].HeaderText = "Dye";
            for (int i = 0; i < Plotter.COL_CNT; i++)
            {
                dgv_opticDatum_tube4.Columns[i + 1].HeaderText = Plotter.Optic_Measure_Idx[i].ToString();
                //dataGridView5.Columns[1].Name = "Threshold";
                //dataGridView5.Columns[2].Name = "Result";
            }

            //dataGridView_Diagnosis.Columns[9].Name = "INH";

            //dataGridView5.Rows[0].DefaultCellStyle.BackColor = Color.AliceBlue;

            string[,] FluoresenceValuesOne = new string[Plotter.DYE_CNT * Plotter.CH_CNT, Plotter.COL_CNT + 1];
            FluoresenceValuesOne[0, 0] = "FAM1";
            FluoresenceValuesOne[1, 0] = "ROX1";
            FluoresenceValuesOne[2, 0] = "HEX1";
            FluoresenceValuesOne[3, 0] = "CY51";
            FluoresenceValuesOne[4, 0] = "FAM2";
            FluoresenceValuesOne[5, 0] = "ROX2";
            FluoresenceValuesOne[6, 0] = "HEX2";
            FluoresenceValuesOne[7, 0] = "CY52";
            FluoresenceValuesOne[8, 0] = "FAM3";
            FluoresenceValuesOne[9, 0] = "ROX3";
            FluoresenceValuesOne[10, 0] = "HEX3";
            FluoresenceValuesOne[11, 0] = "CY53";
            FluoresenceValuesOne[12, 0] = "FAM4";
            FluoresenceValuesOne[13, 0] = "ROX4";
            FluoresenceValuesOne[14, 0] = "HEX4";
            FluoresenceValuesOne[15, 0] = "CY54";

            string[] temp = new string[Plotter.COL_CNT + 1];
            int[] iTemp = new int[Plotter.COL_CNT + 1];

            for (int k = 0; k < Plotter.CH_CNT; k++)
            {
                for (int j = 0; j < Plotter.DYE_CNT; j++)
                {
                    temp[0] = FluoresenceValuesOne[4*k + j, 0];
                    for (int i = 0; i < Plotter.COL_CNT; i++)
                    {
                        if (chkBox_baselineNoScale.Checked)
                        {
                            if (i < Plotter.MaxCycleForBaseCalculation)
                            {
                                temp[i + 1] = "0"; // 0until bas calculated  
                            }
                            else
                            {
                                Int32.TryParse(stringArray[Plotter.CH_CNT * k + j, i], out iTemp[i + 1]);
                                if (iTemp[i + 1] > CtlineVal[j + Plotter.CH_CNT * k])
                                {
                                    //temp[i + 1] = (iTemp[i + 1]).ToString();
                                    temp[i + 1] = ((int)(iTemp[i + 1] - CtlineVal[Plotter.CH_CNT * k + j])).ToString();
                                }
                                else
                                {
                                    temp[i + 1] = "0";
                                }
                                //temp[i + 1] = stringArray[Plotter.CH_CNT * k + j, i];
                            }
                        }
                        else if (chkBox_BaselineScale.Checked)
                        {
                            if (i < Plotter.MaxCycleForBaseCalculation)
                            {
                                temp[i + 1] = "0"; // 0 until bas calculated  
                            }
                            else
                            {
                                Int32.TryParse(stringArray[Plotter.CH_CNT * k + j, i], out iTemp[i + 1]);
                                if (iTemp[i + 1] > BaselineVal[j + Plotter.CH_CNT * k])
                                {
                                    //temp[i + 1] = (iTemp[i + 1]).ToString();
                                    //double scaleFactor = (double)(2500 / (2500 - CtlineVal[Plotter.CH_CNT * k + j]));

                                    //temp[i + 1] = ((iTemp[i + 1] - CtlineVal[Plotter.CH_CNT * k + j]) * scaleFactor).ToString();
                                    temp[i + 1] = ((iTemp[i + 1] - BaselineVal[Plotter.CH_CNT * k + j]) * scaleFactor[j + Plotter.CH_CNT * k]).ToString();
                                }
                                else
                                {
                                    temp[i + 1] = "0";
                                }
                                //temp[i + 1] = stringArray[Plotter.CH_CNT * k + j, i];
                            }
                        }
                        else
                        {
                            Int32.TryParse(stringArray[Plotter.CH_CNT * k + j, i], out iTemp[i + 1]);
                            //if (iTemp[i + 1] > CtlineVal[j + Plotter.CH_CNT * k])
                            //{
                            temp[i + 1] = (iTemp[i + 1]).ToString();
                            //temp[i + 1] = (iTemp[i + 1] - base_calculated[Plotter.CH_CNT*k +j]).ToString();
                            //}
                            //else
                            //{
                            //temp[i + 1] = "0";
                            //}
                        }
                        double dTemp1;
                        dTemp1 = Convert.ToDouble(temp[i + 1]);
                        iTemp2 = (int)dTemp1;
                        temp[i + 1] = iTemp2.ToString();
                        DISPLAY_DATA[Plotter.CH_CNT * k + j, i] = iTemp2.ToString();
                        //DISPLAY_DATA[Plotter.CH_CNT * k + j, i] = (temp[i + 1];
                        FluoresenceValuesOne[Plotter.CH_CNT * k + j, i + 1] = stringArray[Plotter.CH_CNT * k + j, i];
                    }
                    //dgv[k].Rows.Add();
                    dgv[k].Rows.Add(temp);
                }

                dgv[k].Font = new Font("문체부 제목 돋음체", 18, FontStyle.Regular);
                dgv[k].DefaultCellStyle.Font = new Font("문체부 제목 돋음체", 18, FontStyle.Bold);

                // 그리드뷰를 설정
                for (int i = 0; i < dgv[k].Rows.Count; i++)
                {
                    if (i % 2 != 0)
                    {
                        dgv[k].Rows[i].DefaultCellStyle.BackColor = Color.White;
                    }
                    else
                    {
                        dgv[k].Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                    }
                }
            }
        }

        private void updateDataGridOpticDatasWithoutMinMax(DataGridView[] dgv, string[,] stringArray)
        {
            int iTemp2;

            dgv_opticDatum_tube1.Columns[0].HeaderText = "Dye";
            for (int i = 0; i < Plotter.COL_CNT; i++)
            {
                dgv_opticDatum_tube1.Columns[i + 1].HeaderText = Plotter.Optic_Measure_Idx[i].ToString();
                //dataGridView5.Columns[1].Name = "Threshold";
                //dataGridView5.Columns[2].Name = "Result";
            }

            dgv_opticDatum_tube2.Columns[0].HeaderText = "Dye";
            for (int i = 0; i < Plotter.COL_CNT; i++)
            {
                dgv_opticDatum_tube2.Columns[i + 1].HeaderText = Plotter.Optic_Measure_Idx[i].ToString();
                //dataGridView5.Columns[1].Name = "Threshold";
                //dataGridView5.Columns[2].Name = "Result";
            }

            dgv_opticDatum_tube3.Columns[0].HeaderText = "Dye";
            for (int i = 0; i < Plotter.COL_CNT; i++)
            {
                dgv_opticDatum_tube3.Columns[i + 1].HeaderText = Plotter.Optic_Measure_Idx[i].ToString();
                //dataGridView5.Columns[1].Name = "Threshold";
                //dataGridView5.Columns[2].Name = "Result";
            }

            dgv_opticDatum_tube4.Columns[0].HeaderText = "Dye";
            for (int i = 0; i < Plotter.COL_CNT; i++)
            {
                dgv_opticDatum_tube4.Columns[i + 1].HeaderText = Plotter.Optic_Measure_Idx[i].ToString();
                //dataGridView5.Columns[1].Name = "Threshold";
                //dataGridView5.Columns[2].Name = "Result";
            }

            //dataGridView_Diagnosis.Columns[9].Name = "INH";

            //dataGridView5.Rows[0].DefaultCellStyle.BackColor = Color.AliceBlue;

            string[,] FluoresenceValuesOne = new string[Plotter.DYE_CNT * Plotter.CH_CNT, Plotter.COL_CNT + 1];
            FluoresenceValuesOne[0, 0] = "FAM1";
            FluoresenceValuesOne[1, 0] = "ROX1";
            FluoresenceValuesOne[2, 0] = "HEX1";
            FluoresenceValuesOne[3, 0] = "CY51";
            FluoresenceValuesOne[4, 0] = "FAM2";
            FluoresenceValuesOne[5, 0] = "ROX2";
            FluoresenceValuesOne[6, 0] = "HEX2";
            FluoresenceValuesOne[7, 0] = "CY52";
            FluoresenceValuesOne[8, 0] = "FAM3";
            FluoresenceValuesOne[9, 0] = "ROX3";
            FluoresenceValuesOne[10, 0] = "HEX3";
            FluoresenceValuesOne[11, 0] = "CY53";
            FluoresenceValuesOne[12, 0] = "FAM4";
            FluoresenceValuesOne[13, 0] = "ROX4";
            FluoresenceValuesOne[14, 0] = "HEX4";
            FluoresenceValuesOne[15, 0] = "CY54";

            string[] temp = new string[Plotter.COL_CNT + 1];
            int[] iTemp = new int[Plotter.COL_CNT + 1];

            for (int k = 0; k < Plotter.CH_CNT; k++)
            {
                for (int j = 0; j < Plotter.DYE_CNT; j++)
                {
                    temp[0] = FluoresenceValuesOne[j, 0];
                    for (int i = 0; i < Plotter.COL_CNT; i++)
                    {
                        if (chkBox_baselineNoScale.Checked)
                        {
                            if (i < Plotter.MaxCycleForBaseCalculation)
                            {
                                temp[i + 1] = "0"; // 0until bas calculated  
                            }
                            else
                            {
                                Int32.TryParse(stringArray[Plotter.CH_CNT * k + j, i], out iTemp[i + 1]);
                                if (iTemp[i + 1] > CtlineVal[j + Plotter.CH_CNT * k])
                                {
                                    //temp[i + 1] = (iTemp[i + 1]).ToString();
                                    temp[i + 1] = ((int)(iTemp[i + 1] - CtlineVal[Plotter.CH_CNT * k + j])).ToString();
                                }
                                else
                                {
                                    temp[i + 1] = "0";
                                }
                                //temp[i + 1] = stringArray[Plotter.CH_CNT * k + j, i];
                            }
                        }
                        else if (chkBox_BaselineScale.Checked)
                        {
                            if (i < Plotter.MaxCycleForBaseCalculation)
                            {
                                temp[i + 1] = "0"; // 0 until bas calculated  
                            }
                            else
                            {
                                Int32.TryParse(stringArray[Plotter.CH_CNT * k + j, i], out iTemp[i + 1]);
                                if (iTemp[i + 1] > BaselineVal[j + Plotter.CH_CNT * k])
                                {
                                    //temp[i + 1] = (iTemp[i + 1]).ToString();
                                    //double scaleFactor = (double)(2500 / (2500 - CtlineVal[Plotter.CH_CNT * k + j]));
                                    temp[i + 1] = ((iTemp[i + 1] - BaselineVal[Plotter.CH_CNT * k + j]) * scaleFactor[Plotter.CH_CNT * k + j]).ToString();
                                }
                                else
                                {
                                    temp[i + 1] = "0";
                                }
                                //temp[i + 1] = stringArray[Plotter.CH_CNT * k + j, i];
                            }
                        }
                        else
                        {
                            Int32.TryParse(stringArray[Plotter.CH_CNT * k + j, i], out iTemp[i + 1]);
                            //if (iTemp[i + 1] > CtlineVal[j + Plotter.CH_CNT * k])
                            //{
                            temp[i + 1] = (iTemp[i + 1]).ToString();
                            //temp[i + 1] = (iTemp[i + 1] - base_calculated[Plotter.CH_CNT*k +j]).ToString();
                            //}
                            //else
                            //{
                            //temp[i + 1] = "0";
                            //}
                        }
                        double dTemp1;
                        dTemp1 = Convert.ToDouble(temp[i + 1]);
                        iTemp2 = (int)dTemp1;
                        temp[i + 1] = iTemp2.ToString();
                        DISPLAY_DATA[Plotter.CH_CNT * k + j, i] = iTemp2.ToString();
                        //DISPLAY_DATA[Plotter.CH_CNT * k + j, i] = (temp[i + 1];
                        FluoresenceValuesOne[Plotter.CH_CNT * k + j, i + 1] = stringArray[Plotter.CH_CNT * k + j, i];
                    }
                    //dgv[k].Rows.Add();
                    dgv[k].Rows.Add(temp);
                }

                dgv[k].Font = new Font("문체부 제목 돋음체", 18, FontStyle.Regular);
                dgv[k].DefaultCellStyle.Font = new Font("문체부 제목 돋음체", 18, FontStyle.Bold);

                // 그리드뷰를 설정
                for (int i = 0; i < dgv[k].Rows.Count; i++)
                {
                    if (i % 2 != 0)
                    {
                        dgv[k].Rows[i].DefaultCellStyle.BackColor = Color.White;
                    }
                    else
                    {
                        dgv[k].Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                    }
                }
            }
        }



        private void setDataGrid_Ct(DataGridView dgv, int chamberIndex, int columindex, string data1, string data2, string data3, string data4, int[] hValue)
        {
            // chamberIndex : 1~4
            // columindex : base~final (1~8)

            // 기존 그리드 값을 읽어온 후 값을 업데이트 하고 다시 그리드 값을 설정하자
            //string[] chamberName = { "chamber1", "chamber2", "chamber3", "chamber4" };
            string[] d1 = new string[29] { headerName[0], "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
            string[] d2 = new string[29] { headerName[1], "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
            string[] d3 = new string[29] { headerName[2], "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
            string[] d4 = new string[29] { headerName[3], "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };

            //string[] d1 = new string[10] { chamberName[chamberIndex - 1], headerName[0], "0", "0", "0", "0", "0", "0", "0", "0" };
            //string[] d2 = new string[10] { chamberName[chamberIndex - 1], headerName[1], "0", "0", "0", "0", "0", "0", "0", "0" };
            //string[] d3 = new string[10] { chamberName[chamberIndex - 1], headerName[2], "0", "0", "0", "0", "0", "0", "0", "0" };
            //string[] d4 = new string[10] { chamberName[chamberIndex - 1], headerName[3], "0", "0", "0", "0", "0", "0", "0", "0" };

            // 데이터 가져오기
            int rowNo = 0;
            foreach (DataGridViewRow row in dgv.Rows)
            {
                rowNo++;
                if (!row.IsNewRow)
                {
                    for (int i = 0; i < dgv.Columns.Count; i++)
                    {
                        if (rowNo == 1) d1[i] = row.Cells[i].Value.ToString();
                        if (rowNo == 2) d2[i] = row.Cells[i].Value.ToString();
                        if (rowNo == 3) d3[i] = row.Cells[i].Value.ToString();
                        if (rowNo == 4) d4[i] = row.Cells[i].Value.ToString();
                    }
                }
            }

            int toInt;
            float toFloat;

            // 숫자 변환
            float.TryParse(data1, out toFloat);
            int iD1 = (int)toFloat;

            float.TryParse(data2, out toFloat);
            int iD2 = (int)toFloat;

            float.TryParse(data3, out toFloat);
            int iD3 = (int)toFloat;

            float.TryParse(data4, out toFloat);
            int iD4 = (int)toFloat;

            // 데이터 업데이트
            d1[columindex + 1] = iD1.ToString();
            d2[columindex + 1] = iD2.ToString();
            d3[columindex + 1] = iD3.ToString();
            d4[columindex + 1] = iD4.ToString();

            // 새로운 데이터는 추가
            dgv.Rows.Add(d1);
            dgv.Rows.Add(d2);
            dgv.Rows.Add(d3);
            dgv.Rows.Add(d4);

            // 기존 데이터는 삭제
            dgv.Rows.RemoveAt(0);
            dgv.Rows.RemoveAt(0);
            dgv.Rows.RemoveAt(0);
            dgv.Rows.RemoveAt(0);


            // 그리드뷰를 설정
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (i % 2 != 0)
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.White;
                }
                else
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                }
            }

            if (Plotter.isValueBase)
            {
                Plotter.ResetAllPlots(formsPlot1, formsPlot2, formsPlot3, formsPlot4);
                Plotter.isValueBase = false;
            }
            //Plotter.ResetAllPlots(formsPlot1, formsPlot2, formsPlot3, formsPlot4);
            switch (chamberIndex)
            {
                case 1:
                    Plotter.ch1DataDic["FAM"].Add(iD1);
                    Plotter.ch1DataDic["ROX"].Add(iD2);
                    Plotter.ch1DataDic["HEX"].Add(iD3);
                    Plotter.ch1DataDic["CY5"].Add(iD4);
                    Plotter.UpdatePlot(formsPlot1, "Tube1", 0, Plotter.ch1DataDic, cBoxCh1FAM, cBoxCh1ROX, cBoxCh1HEX, cBoxCh1CY5, hValue[0], hValue[1], hValue[2], hValue[3]);
                    break;
                case 2:
                    Plotter.ch2DataDic["FAM"].Add(iD1);
                    Plotter.ch2DataDic["ROX"].Add(iD2);
                    Plotter.ch2DataDic["HEX"].Add(iD3);
                    Plotter.ch2DataDic["CY5"].Add(iD4);
                    Plotter.UpdatePlot(formsPlot2, "Tube2", 1, Plotter.ch2DataDic, cBoxCh2FAM, cBoxCh2ROX, cBoxCh2HEX, cBoxCh2CY5, hValue[4], hValue[5], hValue[6], hValue[7]);
                    break;
                case 3:
                    Plotter.ch3DataDic["FAM"].Add(iD1);
                    Plotter.ch3DataDic["ROX"].Add(iD2);
                    Plotter.ch3DataDic["HEX"].Add(iD3);
                    Plotter.ch3DataDic["CY5"].Add(iD4);
                    Plotter.UpdatePlot(formsPlot3, "Tube3", 2, Plotter.ch3DataDic, cBoxCh3FAM, cBoxCh3ROX, cBoxCh3HEX, cBoxCh3CY5, hValue[8], hValue[9], hValue[10], hValue[11]);
                    break;
                case 4:
                    Plotter.ch4DataDic["FAM"].Add(iD1);
                    Plotter.ch4DataDic["ROX"].Add(iD2);
                    Plotter.ch4DataDic["HEX"].Add(iD3);
                    Plotter.ch4DataDic["CY5"].Add(iD4);
                    Plotter.UpdatePlot(formsPlot4, "Tube4", 3, Plotter.ch4DataDic, cBoxCh4FAM, cBoxCh4ROX, cBoxCh4HEX, cBoxCh4CY5, hValue[12], hValue[13], hValue[14], hValue[15]);
                    break;
                default:
                    break;
            }

        }

        private void setDataGrid_Engineer2(DataGridView dgv, string chName, int index, string data1, string data2, string data3, string data4, string data5, string data6, string data7, string data8)
        {
            // 현재 줄을 지움
            dgv.Rows.RemoveAt(0);

            string[] data_1 = new string[10] { chName, headerName[index], data1, data2, data3, data4, data5, data6, data7, data8 };

            dgv.Rows.Add(data_1);

            // 그리드뷰를 설정
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (i % 2 != 0)
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.White;
                }
                else
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                }
            }
        }
        private void presetDataGrid_Engineer(DataGridView dgv, string value)
        {
            string[] data2 = new string[10] { value, "0", "0", "0", "0", "0", "0", "0", "0", "0" };
            string[] data3 = new string[10] { value, "0", "0", "0", "0", "0", "0", "0", "0", "0" };
            string[] data4 = new string[10] { value, "0", "0", "0", "0", "0", "0", "0", "0", "0" };
            string[] data5 = new string[10] { value, "0", "0", "0", "0", "0", "0", "0", "0", "0" };

            //string[] data2 = new string[10] { value, headerName[0], "0", "0", "0", "0", "0", "0", "0", "0" };
            //string[] data3 = new string[10] { value, headerName[1], "0", "0", "0", "0", "0", "0", "0", "0" };
            //string[] data4 = new string[10] { value, headerName[2], "0", "0", "0", "0", "0", "0", "0", "0" };
            //string[] data5 = new string[10] { value, headerName[3], "0", "0", "0", "0", "0", "0", "0", "0" };

            dgv.Rows.Add(data2);
            dgv.Rows.Add(data3);
            dgv.Rows.Add(data4);
            dgv.Rows.Add(data5);

            dgv.Font = new Font("문체부 제목 돋음체", 18, FontStyle.Regular);
            dgv.DefaultCellStyle.Font = new Font("문체부 제목 돋음체", 18, FontStyle.Bold);

            // 그리드뷰를 설정
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (i % 2 != 0)
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.White;
                }
                else
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                }
            }
        }

        private string[] SetArrayRandomly(string[] stringArray, string header, int maxIndex)
        {
            int rand = 0;
            stringArray = new string[maxIndex];

            stringArray[0] = header;
            for (int i = 1; i < maxIndex; i++)
            {
                rand = r.Next(500, 2500);
                stringArray[i] = rand.ToString();
            }
            return stringArray;
        }

        private void presetDataGrid_ct_randomly(DataGridView dgv, string value)
        {
            string[] data1 = new string[30];
            data1 = SetArrayRandomly(data1, headerName[0], 30);
            string[] data2 = new string[30];
            data2 = SetArrayRandomly(data2, headerName[1], 30);
            string[] data3 = new string[30];
            data3 = SetArrayRandomly(data3, headerName[2], 30);
            string[] data4 = new string[30];
            data4 = SetArrayRandomly(data4, headerName[3], 30);


            dgv.Rows.Add(data1);
            dgv.Rows.Add(data2);
            dgv.Rows.Add(data3);
            dgv.Rows.Add(data4);

            dgv.Font = new Font("문체부 제목 돋음체", 18, FontStyle.Regular);
            dgv.DefaultCellStyle.Font = new Font("문체부 제목 돋음체", 18, FontStyle.Bold);

            // 그리드뷰를 설정
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (i % 2 != 0)
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.White;
                }
                else
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                }
            }
        }

        private void presetDataGrid_ct(DataGridView dgv, string value)
        {
            /*
            string[] data1 = new string[30];
            data1 = SetArrayRandomly(data1, headerName[0], 30);
            string[] data2 = new string[30];
            data2 = SetArrayRandomly(data2, headerName[1], 30);
            string[] data3 = new string[30];
            data3 = SetArrayRandomly(data3, headerName[2], 30);
            string[] data4 = new string[30];
            data4 = SetArrayRandomly(data4, headerName[3], 30);
            */

            /*
            string[] data1 = new string[30];
            data1[0] = headerName[0];
            for(int i = 1; i < 30; i++ )
            {
                Random r = new Random();
                int rand = r.Next(500, 2500);

                data1[i] = rand.ToString();
            }
            */
            //dahunj

            string[] data1 = new string[30] { headerName[0], "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
            string[] data2 = new string[30] { headerName[1], "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
            string[] data3 = new string[30] { headerName[2], "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
            string[] data4 = new string[30] { headerName[3], "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };


            dgv.Rows.Add(data1);
            dgv.Rows.Add(data2);
            dgv.Rows.Add(data3);
            dgv.Rows.Add(data4);

            dgv.Font = new Font("문체부 제목 돋음체", 18, FontStyle.Regular);
            dgv.DefaultCellStyle.Font = new Font("문체부 제목 돋음체", 18, FontStyle.Bold);

            // 그리드뷰를 설정
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (i % 2 != 0)
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.White;
                }
                else
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                }
            }
        }

        private void setDataGrid_Tester(DataGridView dgv, int chamberIndex, string data1, string data2, string data3, string data4)
        {
            // 현재 줄을 지움
            dgv.Rows.RemoveAt(0);

            // string[] chamberName = { "chamber1", "chamber2", "chamber3", "chamber4" };

            int toInt;
            float toFloat;

            // 숫자 변환
            float.TryParse(data1, out toFloat); int iD1 = (int)toFloat;
            float.TryParse(data2, out toFloat); int iD2 = (int)toFloat;
            float.TryParse(data3, out toFloat); int iD3 = (int)toFloat;
            float.TryParse(data4, out toFloat); int iD4 = (int)toFloat;


            // 다시 업데이트
            string[] data = new string[5] { chamberName[chamberIndex - 1], iD1.ToString(), iD2.ToString(), iD3.ToString(), iD4.ToString() };

            dgv.Rows.Add(data);
            dgv.Rows[0].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
        }
        private void presetDataGrid_Tester(DataGridView dgv, string value, string value1, string value2, string value3, string value4)
        {
            string[] data1 = new string[5] { value, value1, value2, value3, value4 };

            dgv.Rows.Add(data1);

            dgv.Font = new Font("문체부 제목 돋음체", 28, FontStyle.Regular);
            dgv.DefaultCellStyle.Font = new Font("문체부 제목 돋음체", 28, FontStyle.Bold);

            dgv.CurrentCell = null;

            dgv.Rows[0].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
        }
        private void setHeaderName()
        {
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists) di.Create();

            string fileName = di.ToString() + "\\header.info";

            string[] lines = File.ReadAllLines(fileName);

            string temp2 = "", temp0 = "", temp1 = "";
            temp0 = lines[1]; // 검사자용
            temp1 = lines[2]; // 관리자용
            temp2 = lines[0]; // 챔버이름


            char[] sep = { ',' };

            string[] result2 = temp2.Split(sep);
            chamberName[0] = result2[0];
            chamberName[1] = result2[1];
            chamberName[2] = result2[2];
            chamberName[3] = result2[3];

            string[] result0 = temp0.Split(sep);
            dataGridView1_Tester.Columns[0].HeaderText = result0[0];
            dataGridView1_Tester.Columns[1].HeaderText = result0[1];
            dataGridView1_Tester.Columns[2].HeaderText = result0[2];
            dataGridView1_Tester.Columns[3].HeaderText = result0[3];
            dataGridView1_Tester.Columns[4].HeaderText = result0[4];

            dataGridView2_Tester.Columns[0].HeaderText = result0[0];
            dataGridView2_Tester.Columns[1].HeaderText = result0[1];
            dataGridView2_Tester.Columns[2].HeaderText = result0[2];
            dataGridView2_Tester.Columns[3].HeaderText = result0[3];
            dataGridView2_Tester.Columns[4].HeaderText = result0[4];

            dataGridView3_Tester.Columns[0].HeaderText = result0[0];
            dataGridView3_Tester.Columns[1].HeaderText = result0[1];
            dataGridView3_Tester.Columns[2].HeaderText = result0[2];
            dataGridView3_Tester.Columns[3].HeaderText = result0[3];
            dataGridView3_Tester.Columns[4].HeaderText = result0[4];

            dataGridView4_Tester.Columns[0].HeaderText = result0[0];
            dataGridView4_Tester.Columns[1].HeaderText = result0[1];
            dataGridView4_Tester.Columns[2].HeaderText = result0[2];
            dataGridView4_Tester.Columns[3].HeaderText = result0[3];
            dataGridView4_Tester.Columns[4].HeaderText = result0[4];

            headerName[0] = result0[1];
            headerName[1] = result0[2];
            headerName[2] = result0[3];
            headerName[3] = result0[4];


            string[] result1 = temp1.Split(sep);
            for (int i = 0; i < 10; i++)
            {
                dataGridView1_Engineer.Columns[i].HeaderText = result1[i];
            }
            for (int i = 0; i < 10; i++)
            {
                dataGridView2_Engineer.Columns[i].HeaderText = result1[i];
            }
            for (int i = 0; i < 10; i++)
            {
                dataGridView3_Engineer.Columns[i].HeaderText = result1[i];
            }
            for (int i = 0; i < 10; i++)
            {
                dataGridView4_Engineer.Columns[i].HeaderText = result1[i];
            }

            //


            // 그리드 컬럼 우측 정렬
            //            dataGridView4_Engineer.DataGrid

        }
        private void setUserMode(int um)
        {
            userMode = um;

            if (userMode != 0) btn_Login_Main.Text = "LOG OUT";

            tb_Log.Visible = false;


            if (userMode == 0)
            {
                tabControl1.SelectedIndex = 0;
                tb_WorkerID_Test.Text = "";
                tb_WorkerName_Test.Text = "";
            }
            // 검사자이면 검사탭으로 이동
            if (userMode == 1)
            {
                tabControl1.SelectedIndex = 1;
            }
            // 관리자이면 관리자탬으로 이동
            else if (userMode == 2)
            {
                tabControl1.SelectedIndex = 2;
            }
            // 엔지니어면 엔지니어탭으로 이동
            else if (userMode == 3)
            {
                tabControl1.SelectedIndex = 3;
                tb_Log.Visible = true;
            }

            //tb_PW_Main.Text = "";
        }
        private void setProcessMode(int pm)
        {
            switch (pm)
            {
                case 0:  // 검사 초기화
                    processStep = 0;
                    progressBar_step.Value = 0 / 8;
                    tb_PatientID_Test.Text = "";
                    tb_CartridgeID_Test.Text = "";
                    tb_SampleID_Test.Text = "";

                    // 조건에 따른 버튼 등 활성화
                    if (checkBox2.Checked)
                    {
                        cb_Recipe_Test.Enabled = true;
                        btn_SetRecipe_Test.Enabled = true;
                        btn_Start_Test.Enabled = true;

                        tb_PatientID_Test.Enabled = true;
                        tb_SampleID_Test.Enabled = true;
                        tb_CartridgeID_Test.Enabled = true;

                        btn_OpenDoor_Test.Enabled = true;
                        btn_DoorOpen_Test.Enabled = true;

                    }
                    else
                    {
                        cb_Recipe_Test.Enabled = false;
                        btn_SetRecipe_Test.Enabled = false;
                        //btn_Start_Test.Enabled = false;

                        //tb_PatientID_Test.Enabled = false;
                        //tb_SampleID_Test.Enabled = false;
                        //tb_CartridgeID_Test.Enabled = false;

                        btn_OpenDoor_Test.Enabled = true;
                        btn_DoorOpen_Test.Enabled = true;

                    }

                    break;
                case 1:   // 검사를 위해 문을 염
                    processStep = 1;
                    progressBar_step.Value = 100 / 8;
                    break;
                case 2:   // 정의 삭제됨 - 바로 3으로
                    processStep = 2;
                    progressBar_step.Value = 200 / 8;
                    break;
                case 3:   // ID 정보 입력 단계
                    processStep = 3;
                    progressBar_step.Value = 300 / 8;

                    // 조건에 따른 버튼 등 활성화
                    if (checkBox2.Checked)
                    {
                        cb_Recipe_Test.Enabled = true;
                        btn_SetRecipe_Test.Enabled = true;
                        btn_Start_Test.Enabled = true;

                        tb_PatientID_Test.Enabled = true;
                        tb_SampleID_Test.Enabled = true;
                        tb_CartridgeID_Test.Enabled = true;

                        btn_OpenDoor_Test.Enabled = true;

                    }
                    else
                    {
                        cb_Recipe_Test.Enabled = false;
                        btn_SetRecipe_Test.Enabled = false;
                        //btn_Start_Test.Enabled = false;

                        tb_PatientID_Test.Enabled = true;
                        tb_SampleID_Test.Enabled = true;
                        tb_CartridgeID_Test.Enabled = true;

                        btn_OpenDoor_Test.Enabled = false;

                    }

                    break;
                case 4:  // ID 정보 입력 끝남
                    processStep = 4;
                    progressBar_step.Value = 400 / 8;

                    // 조건에 따른 버튼 등 활성화
                    if (checkBox2.Checked)
                    {
                        cb_Recipe_Test.Enabled = true;
                        btn_SetRecipe_Test.Enabled = true;
                        btn_Start_Test.Enabled = true;

                        tb_PatientID_Test.Enabled = true;
                        tb_SampleID_Test.Enabled = true;
                        tb_CartridgeID_Test.Enabled = true;

                        btn_OpenDoor_Test.Enabled = true;
                    }
                    else
                    {
                        cb_Recipe_Test.Enabled = true;
                        btn_SetRecipe_Test.Enabled = true;
                        //btn_Start_Test.Enabled = false;

                        tb_PatientID_Test.Enabled = true;
                        tb_SampleID_Test.Enabled = true;
                        tb_CartridgeID_Test.Enabled = true;

                        btn_OpenDoor_Test.Enabled = false;

                    }

                    break;
                case 5:   // 카트리지 캡 제거 안내
                    processStep = 5;
                    progressBar_step.Value = 500 / 8;
                    break;
                case 6:     // 팁 장착 안내 전
                    processStep = 6;
                    progressBar_step.Value = 600 / 8;
                    break;
                case 7:    // 팁 장착 안내 후
                    processStep = 7;
                    progressBar_step.Value = 700 / 8;
                    break;
                case 8:    // 준비 완료
                    processStep = 8;
                    progressBar_step.Value = 800 / 8;

                    // 조건에 따른 버튼 등 활성화
                    if (checkBox2.Checked)
                    {
                        cb_Recipe_Test.Enabled = true;
                        btn_SetRecipe_Test.Enabled = true;
                        btn_Start_Test.Enabled = true;

                        tb_PatientID_Test.Enabled = true;
                        tb_SampleID_Test.Enabled = true;
                        tb_CartridgeID_Test.Enabled = true;

                        btn_OpenDoor_Test.Enabled = true;
                    }
                    else
                    {
                        cb_Recipe_Test.Enabled = true;
                        btn_SetRecipe_Test.Enabled = true;
                        btn_Start_Test.Enabled = true;

                        tb_PatientID_Test.Enabled = true;
                        tb_SampleID_Test.Enabled = true;
                        tb_CartridgeID_Test.Enabled = true;

                        btn_OpenDoor_Test.Enabled = false;
                    }
                    break;
                case 9:   // 검사 중
                    processStep = 9;
                    progressBar_step.Value = 800 / 8;

                    // 조건에 따른 버튼 등 활성화
                    if (checkBox2.Checked)
                    {
                        cb_Recipe_Test.Enabled = true;
                        btn_SetRecipe_Test.Enabled = true;
                        btn_Start_Test.Enabled = true;

                        tb_PatientID_Test.Enabled = true;
                        tb_SampleID_Test.Enabled = true;
                        tb_CartridgeID_Test.Enabled = true;

                        btn_OpenDoor_Test.Enabled = true;
                        btn_DoorOpen_Test.Enabled = true;
                    }
                    else
                    {
                        cb_Recipe_Test.Enabled = false;
                        btn_SetRecipe_Test.Enabled = false;
                        btn_Start_Test.Enabled = true;

                        tb_PatientID_Test.Enabled = false;
                        tb_SampleID_Test.Enabled = false;
                        tb_CartridgeID_Test.Enabled = false;

                        btn_OpenDoor_Test.Enabled = false;
                        btn_DoorOpen_Test.Enabled = false;
                    }
                    break;
            }

            //MessageBox.Show(pm.ToString());
        }

        struct DataParameter
        {
            public int Delay;
        }

        private DataParameter _inputparameter;

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            int delay = ((DataParameter)e.Argument).Delay;
            int index = 0;
            try
            {
                if (!backgroundWorker1.CancellationPending)
                {

                    Thread.Sleep(delay);
                }

            }
            catch (Exception ex)
            {
                backgroundWorker1.CancelAsync();
                Console.WriteLine(ex.StackTrace);
                //MessageBox.Show(ex.Message, "Message", MessageBoxButtons.OK, MessageBoxIon.Error);
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //progress.Value = e.ProgressPercentage;
            //progress.Update();
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                string cTime = DateTime.Now.ToLongTimeString();
                string cDate = DateTime.Now.ToLongDateString();
                this.Text = "ABI POC PCR ver. 2.0.0     [" + cDate + " | " + cTime + "]";

                // 데이터 버퍼가 찼는지 확인하고 업데이트
                nTimerNo++;

                if (processStep == 9 && nTimerNo % 500 == 0) // 5초 마다 업데이트
                {
                    updateCycleCount();

                    FindCyclesForBaseCalculation();
                    sm.DataUpdateFlag = true;
                    updateDataNGraph();
                    nTimerNo = 0;

                    if (sm.ProcessEndFlag)// && isOpticDataLastBufferFilled())
                    {
                        sm.ProcessEndFlag = false;
                        _endProcess();
                    }
                }


                if (processStep == 9 && routine_cnt > 0 && nTimerNo % 100 == 0) // 5초 마다 업데이트
                {
                    //nTimerNo = 0;
                    //if(routine_cnt == 7 || routine_cnt == 8  )
                    //    btnSetBaseData_Click(null, null);
                    //else if (routine_cnt > 8)
                    //    btnSetFinalData_Click(null, null);
                }

                if (processStep == 9) // 5초 마다 업데이트
                {
                    // 상태바 업데이트 - 시작을 20%에서 시작함
                    sm.progressSecond = (int)(routine_cnt * 2);
                    progressBar_Tester.Value = sm.progressFirst + sm.progressSecond; //(int)(routine_cnt * 2);
                    progressBar_Manager.Value = sm.progressFirst + sm.progressSecond;// (int)(routine_cnt * 2);
                }

                // 버퍼에 쓰레기가 있을 경우 체크해서 클리어
                if (waitData.Length != 0)
                    if (waitData[0].Equals('-') || waitData[0].Equals('<') || waitData[0].Equals('>'))
                        waitData = "";

                if (waitData.Length > 100) waitData = "";


                if (b_start_Process && processStep != 9)
                {
                    b_start_Process = false;
                    _start_Process();
                }

                if (b_check_Door)
                {
                    b_check_Door = false;
                    _check_Door();
                }

                if (b_GUI_Init)
                {
                    // 검사단계 초기화
                    setProcessMode(0);
                    setProgressInfo(-1);
                    btn_Start_Test.Text = "Test Start";

                    b_GUI_Init = false;
                }
                //testButton.Text = routine_cnt.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void updateScottPlot(int tube_idx, double[] hValue)
        {
            // chamberIndex : 1~4
            // columindex : base~final (1~8)

            // 기존 그리드 값을 읽어온 후 값을 업데이트 하고 다시 그리드 값을 설정하자
            //string[] chamberName = { "chamber1", "chamber2", "chamber3", "chamber4" };
            string[] d1 = new string[Plotter.COL_CNT];
            string[] d2 = new string[Plotter.COL_CNT];
            string[] d3 = new string[Plotter.COL_CNT];
            string[] d4 = new string[Plotter.COL_CNT];

            for (int w = 0; w < Plotter.COL_CNT; w++)
            {
                d1[w] = DISPLAY_DATA[Plotter.CH_CNT * (tube_idx), w];
                //d1[w] = ((int)(Convert.ToDouble(MEASURED_DATA[Plotter.CH_CNT *(tube_idx) , w]) - BaselineVal[Plotter.CH_CNT * (tube_idx)])*scaleFactor[Plotter.CH_CNT * (tube_idx)]).ToString();
                if (d1[w] == "0") d1[w] = null;
            }
            for (int w = 0; w < Plotter.COL_CNT; w++)
            {
                d2[w] = DISPLAY_DATA[Plotter.CH_CNT * (tube_idx) + 1, w];
                //d2[w] = MEASURED_DATA[Plotter.CH_CNT * (tube_idx) + 1, w];
                //d2[w] = ((int)(Convert.ToDouble(MEASURED_DATA[Plotter.CH_CNT * (tube_idx)+1, w]) - BaselineVal[Plotter.CH_CNT * (tube_idx)+1]) * scaleFactor[Plotter.CH_CNT * (tube_idx)+1]).ToString();
                if (d2[w] == "0") d2[w] = null;
            }
            for (int w = 0; w < Plotter.COL_CNT; w++)
            {
                d3[w] = DISPLAY_DATA[Plotter.CH_CNT * (tube_idx) + 2, w];
                //d3[w] = MEASURED_DATA[Plotter.CH_CNT * (tube_idx ) + 2, w];
                //d3[w] = ((int)(Convert.ToDouble(MEASURED_DATA[Plotter.CH_CNT * (tube_idx) + 2, w]) - BaselineVal[Plotter.CH_CNT * (tube_idx) + 2]) * scaleFactor[Plotter.CH_CNT * (tube_idx) + 2]).ToString();

                if (d3[w] == "0") d3[w] = null;
            }
            for (int w = 0; w < Plotter.COL_CNT; w++)
            {
                d4[w] = DISPLAY_DATA[Plotter.CH_CNT * (tube_idx) + 3, w];
                //d4[w] = ((int)(Convert.ToDouble(MEASURED_DATA[Plotter.CH_CNT * (tube_idx) + 3, w]) - BaselineVal[Plotter.CH_CNT * (tube_idx) + 3]) * scaleFactor[Plotter.CH_CNT * (tube_idx) + 3]).ToString();

                if (d4[w] == "0") d4[w] = null;
            }


            int[] iD1 = new int[Plotter.COL_CNT];
            int[] iD2 = new int[Plotter.COL_CNT];
            int[] iD3 = new int[Plotter.COL_CNT];
            int[] iD4 = new int[Plotter.COL_CNT];

            //for(int q=0; q < sm.measured_cnt ; q++)
            for (int q = 0; q < Plotter.COL_CNT; q++)
            {
                Int32.TryParse(d1[q], out iD1[q]);
                Int32.TryParse(d2[q], out iD2[q]);
                Int32.TryParse(d3[q], out iD3[q]);
                Int32.TryParse(d4[q], out iD4[q]);
            }


            //for (int i = 0; i < Plotter.COL_CNT; i++)

            for(int i = 0; i < sm.measured_cnt; i++)
            {
                switch (tube_idx + 1)
                {
                    case 1:
                        Plotter.ch1DataDic["FAM"].Add(iD1[i]);
                        Plotter.ch1DataDic["ROX"].Add(iD2[i]);
                        Plotter.ch1DataDic["HEX"].Add(iD3[i]);
                        Plotter.ch1DataDic["CY5"].Add(iD4[i]);
                        Plotter.UpdatePlot(formsPlot1, "Tube1", 0, Plotter.ch1DataDic, cBoxCh1FAM, cBoxCh1ROX, cBoxCh1HEX, cBoxCh1CY5, (int)hValue[0], (int)hValue[1], (int)hValue[2], (int)hValue[3]);
                        break;
                    case 2:
                        Plotter.ch2DataDic["FAM"].Add(iD1[i]);
                        Plotter.ch2DataDic["ROX"].Add(iD2[i]);
                        Plotter.ch2DataDic["HEX"].Add(iD3[i]);
                        Plotter.ch2DataDic["CY5"].Add(iD4[i]);
                        Plotter.UpdatePlot(formsPlot2, "Tube2", 1, Plotter.ch2DataDic, cBoxCh2FAM, cBoxCh2ROX, cBoxCh2HEX, cBoxCh2CY5, (int)hValue[4], (int)hValue[5], (int)hValue[6], (int)hValue[7]);
                        break;
                    case 3:
                        Plotter.ch3DataDic["FAM"].Add(iD1[i]);
                        Plotter.ch3DataDic["ROX"].Add(iD2[i]);
                        Plotter.ch3DataDic["HEX"].Add(iD3[i]);
                        Plotter.ch3DataDic["CY5"].Add(iD4[i]);
                        Plotter.UpdatePlot(formsPlot3, "Tube3", 2, Plotter.ch3DataDic, cBoxCh3FAM, cBoxCh3ROX, cBoxCh3HEX, cBoxCh3CY5, (int)hValue[8], (int)hValue[9], (int)hValue[10], (int)hValue[11]);
                        break;
                    case 4:
                        Plotter.ch4DataDic["FAM"].Add(iD1[i]);
                        Plotter.ch4DataDic["ROX"].Add(iD2[i]);
                        Plotter.ch4DataDic["HEX"].Add(iD3[i]);
                        Plotter.ch4DataDic["CY5"].Add(iD4[i]);
                        Plotter.UpdatePlot(formsPlot4, "Tube4", 3, Plotter.ch4DataDic, cBoxCh4FAM, cBoxCh4ROX, cBoxCh4HEX, cBoxCh4CY5, (int)hValue[12], (int)hValue[13], (int)hValue[14], (int)hValue[15]);
                        break;
                    default:
                        break;
                }
            }
        }

        public void initBaseValue()
        {
            for (int i = 0; i < 16; i++)
            {
                dgv_interpretation_ct.Rows[i].Cells[3].Value = "";
            }
        }


        public void setBaseValueToDataGridView(DataGridView dgv, double[] baseValues)
        {
            for (int i = 0; i < 16; i++)
            {
                dgv.Rows[i].Cells[3].Value = baseValues[i].ToString();
            }
        }


        public void setCtValueToDGV(DataGridView dgv, double[] CtValue)
        {
            for(int i = 0; i < 16; i++)
            {
                dgv.Rows[i].Cells[3].Value = CtValue[i].ToString();
            }
        }

        public void setCtResultToDGV(DataGridView dgv)
        {
            for (int i = 0; i < 16; i++)
            {
                if( Convert.ToDouble( dgv.Rows[i].Cells[3].FormattedValue) > 0)
                    dgv.Rows[i].Cells[2].Value = "+";
                else
                    dgv.Rows[i].Cells[2].Value = "-";
            }
        }

        public void FindCyclesForBaseCalculation()
        {
            //dgv_CycleForBaseCalculation.Rows[0].Cells[0].Value = "";
            //Plotter.isThisCycleWouldbeUsedForBaseCal[0] = dgv_CycleForBaseCalculation.Rows[0].Cells[0].FormattedValue.ToString();
            try
            {
                Plotter.CycleCntForBaseCal = 0;
                for (int i = 0; i < 45; i++)
                {
                    if (dgv_CycleForBaseCalculation.Rows[0].Cells[i].FormattedValue.ToString() == "v"
                        || dgv_CycleForBaseCalculation.Rows[0].Cells[i].FormattedValue.ToString() == "V")
                    {
                        Plotter.CycleCntForBaseCal++;
                        Plotter.isThisCycleWouldbeUsedForBaseCal[i] = 1;
                        Plotter.MaxCycleForBaseCal = i;
                    }
                    else
                    {
                        Plotter.isThisCycleWouldbeUsedForBaseCal[i] = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        public double CtCycleCal()
        {
            return 0.01;
        }

        public double[] baseCalculationDeviation_WithoutMinMax()
        {
            int temp;
            double max = 0;
            double min = 2500;
            double[] base_avg = new double[Plotter.CH_CNT * Plotter.DYE_CNT];
            double[] base_temp = new double[Plotter.COL_CNT];
            double[] base_deviation = new double[Plotter.CH_CNT * Plotter.DYE_CNT];


            for (int l = 0; l < Plotter.COL_CNT; l++)
            {
                base_temp[l] = 0;
            }


            for (int k = 0; k < Plotter.CH_CNT * Plotter.DYE_CNT; k++)
            {
                //BaselineVal[k] = 0;
                base_avg[k] = 0;
            }

            for (int i = 0; i < 16; i++)
            {
                string sigma_scale = dgv_interpretation_ct.Rows[i].Cells[1].FormattedValue.ToString();

                double fSigma_scale;
                fSigma_scale = Convert.ToDouble(sigma_scale);
                //Int32.TryParse(ct_temp, out iCt_temp);
                for (int j = 0; j < Plotter.COL_CNT; j++)
                {
                    if (Plotter.isThisCycleWouldbeUsedForBaseCal[j] == 1)
                    {
                        if (j > Plotter.MaxCycleForBaseCalculation)
                            Plotter.MaxCycleForBaseCalculation = j;

                        base_temp[j] = Convert.ToDouble(MEASURED_DATA[i, j]);
                        if (base_temp[j] > max) max = base_temp[j];
                        if (base_temp[j] < min) min = base_temp[j];
                        //Int32.TryParse(MEASURED_DATA[i, j], out temp);
                        base_avg[i] += base_temp[j];
                    }
                    else
                    {
                        base_temp[j] = 0;
                    }
                }

                base_avg[i] = (base_avg[i]) / (Plotter.CycleCntForBaseCal);


                for (int m = 0; m < Plotter.COL_CNT; m++)
                {
                    if (base_temp[m] > 0)
                    {
                        base_deviation[i] += (base_temp[m] - base_avg[i]) * (base_temp[m] - base_avg[i]);
                    }
                }
                base_deviation[i] /= Plotter.CycleCntForBaseCal;
                base_deviation[i] = Math.Pow(base_deviation[i], 0.5);
                //Int32.TryParse(MEASURED_DATA[i, 0], out ic_value[i]);
                //CtlineVal[i] = (double)(iCt_temp * BaselineVal[i]);
                //ic_value[i] = MEASURED_DATA[i, 0];
                CtlineVal[i] = base_avg[i] + (fSigma_scale * base_deviation[i]);
            }
            return base_deviation;
        }



        public double[] baseCalculationDeviation()
        {
            int temp;

            double[] base_avg = new double[Plotter.CH_CNT * Plotter.DYE_CNT];
            double[] base_temp = new double[Plotter.COL_CNT];
            double[] base_deviation = new double[Plotter.CH_CNT * Plotter.DYE_CNT];

            double[] temp_max = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            double[] temp_min = new double[16] { 2500, 2500, 2500, 2500, 2500, 2500, 2500, 2500, 2500, 2500, 2500, 2500, 2500, 2500, 2500, 2500 };
            double[] temp_pre = new double[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };


            for (int l = 0; l < Plotter.COL_CNT; l++)
            {
                base_temp[l] = 0;
            }


            for (int k = 0; k < Plotter.CH_CNT * Plotter.DYE_CNT; k++)
            {
                //BaselineVal[k] = 0;
                base_avg[k] = 0;
            }

            for (int i = 0; i < 16; i++)
            {
                string sigma_scale = dgv_interpretation_ct.Rows[i].Cells[1].FormattedValue.ToString();

                double fSigma_scale;
                fSigma_scale = Convert.ToDouble(sigma_scale);
                //Int32.TryParse(ct_temp, out iCt_temp);
                for (int j = 0; j < Plotter.COL_CNT; j++)
                {
                    if (Plotter.isThisCycleWouldbeUsedForBaseCal[j] == 1)
                    {
                        if (j > Plotter.MaxCycleForBaseCalculation)
                            Plotter.MaxCycleForBaseCalculation = j;


                        base_temp[j] = Convert.ToDouble(MEASURED_DATA[i, j]);
                        if (base_temp[j] > temp_max[i]) temp_max[i] = base_temp[j];
                        if (base_temp[j] < temp_min[i]) temp_min[i] = base_temp[j];

                        //Int32.TryParse(MEASURED_DATA[i, j], out temp);
                        base_avg[i] += base_temp[j];

                    }
                    else
                    {
                        base_temp[j] = 0;
                    }
                }
                base_avg[i] -= temp_max[i];
                base_avg[i] -= temp_min[i];

                base_avg[i] = (base_avg[i] ) / (Plotter.CycleCntForBaseCal -2 );
                BaselineVal[i] = base_avg[i];

                for (int m = 0; m < Plotter.COL_CNT; m++)
                {
                    if (Plotter.isThisCycleWouldbeUsedForBaseCal[m] == 1)
                    {
                        base_temp[m] = Convert.ToDouble(MEASURED_DATA[i, m]);

                        base_deviation[i] += (base_temp[m] - base_avg[i]) * (base_temp[m] - base_avg[i]);
                    }
                }

                base_deviation[i] -= ((temp_max[i] - base_avg[i]) * (temp_max[i] - base_avg[i]));
                base_deviation[i] -= ((temp_min[i] - base_avg[i]) * (temp_min[i] - base_avg[i]));

                base_deviation[i] /= (Plotter.CycleCntForBaseCal-2);
                base_deviation[i] = Math.Pow(base_deviation[i], 0.5);
                //Int32.TryParse(MEASURED_DATA[i, 0], out ic_value[i]);
                //CtlineVal[i] = (double)(iCt_temp * BaselineVal[i]);
                //ic_value[i] = MEASURED_DATA[i, 0];
                CtlineVal[i] = base_avg[i] + (fSigma_scale * base_deviation[i]);
            }
            return base_deviation;
        }


        public void baseCalculationAverage()
        {
            int temp;
            double temp_max = 0;
            double temp_min = 0;
            double temp_pre = 0;
            
            for (int k = 0; k < 16; k++)
            {
                BaselineVal[k] = 0;
            }

            for (int i = 0; i < 16; i++)
            {
                string ct_scale = dgv_interpretation_ct.Rows[i].Cells[1].FormattedValue.ToString();

                double iCt_scale;
                iCt_scale = Convert.ToDouble(ct_scale);
                //Int32.TryParse(ct_temp, out iCt_temp);
                for (int j = 0; j < Plotter.COL_CNT; j++)
                {
                    if (Plotter.isThisCycleWouldbeUsedForBaseCal[j] == 1)
                    {
                        if (j > Plotter.MaxCycleForBaseCalculation)
                        {
                            Plotter.MaxCycleForBaseCalculation = j;
                            break;
                        }
                        else
                        {
                            Plotter.MaxCycleForBaseCalculation = 0;
                        }

                        double data_temp;

                        data_temp = Convert.ToDouble(MEASURED_DATA[i, j]);
                        if (data_temp > temp_pre)
                        {
                            temp_max = data_temp;
                        }

                        if(data_temp < temp_pre)
                        {
                            temp_min = data_temp;
                        }
                        temp_pre = data_temp;
                        
                        //Int32.TryParse(MEASURED_DATA[i, j], out temp);
                        BaselineVal[i] += data_temp;
                    }
                }
                BaselineVal[i] = ( BaselineVal[i]) / (Plotter.CycleCntForBaseCal ); // 2: min, max    two 
                //Int32.TryParse(MEASURED_DATA[i, 0], out ic_value[i]);
                //CtlineVal[i] = (double)(iCt_scale * BaselineVal[i]);
                //ic_value[i] = MEASURED_DATA[i, 0];
            }
        }

        public void baseCalculation_Avg_WithOutMinAndMax()
        {
            int temp;
            double max = 0;
            double min = 2500;
            for (int k = 0; k < 16; k++)
            {
                BaselineVal[k] = 0;
            }

            for (int i = 0; i < 16; i++)
            {
                string ct_scale = dgv_interpretation_ct.Rows[i].Cells[1].FormattedValue.ToString();

                double iCt_scale;
                iCt_scale = Convert.ToDouble(ct_scale);
                //Int32.TryParse(ct_temp, out iCt_temp);
                for (int j = 0; j < Plotter.COL_CNT; j++)
                {
                    if (Plotter.isThisCycleWouldbeUsedForBaseCal[j] == 1)
                    {
                        if (j > Plotter.MaxCycleForBaseCalculation)
                            Plotter.MaxCycleForBaseCalculation = j;

                        double data_temp;

                        data_temp = Convert.ToDouble(MEASURED_DATA[i, j]);
                        if (data_temp > max)
                            max = data_temp;

                        if (data_temp < min)
                            min = data_temp;

                        //Int32.TryParse(MEASURED_DATA[i, j], out temp);
                        BaselineVal[i] += data_temp;
                    }
                }



                BaselineVal[i] = (BaselineVal[i] - min - max) / (Plotter.CycleCntForBaseCal - 2);
                //Int32.TryParse(MEASURED_DATA[i, 0], out ic_value[i]);
                CtlineVal[i] = (double)(iCt_scale * BaselineVal[i]);
                //ic_value[i] = MEASURED_DATA[i, 0];
            }
        }



        public double[] CtCyclesCalculation()
        {
            double tempCt = 0;
            double value = 0;

            int baseCalculationMaxCycle = 0;

            for(int k = 0; k < Plotter.CYCLE_CNT - 1; k++)
            {
                if (Plotter.isThisCycleWouldbeUsedForBaseCal[k] == 1)
                    baseCalculationMaxCycle = k + 3;
            }
                

            for (int i = 0; i < 16; i++)
            {
                tempCt = 0;
                for (int j = 0; j < Plotter.COL_CNT; j++)
                {
                    value = (Convert.ToDouble(MEASURED_DATA[i, j]) - BaselineVal[i]) * scaleFactor[i];
                    if (value < 0) value = 0;//if (Convert.ToDouble(MEASURED_DATA[i, j]) > CtlineVal[i])
                    else if ((Convert.ToDouble(MEASURED_DATA[i, j]) < Convert.ToDouble(sm.ct_Limit[i])))
                    {
                        Plotter.CtCycles[i] = 0;
                    }
                    else if (value < (CtlineVal[i] - BaselineVal[i]) * scaleFactor[i])
                    {
                        Plotter.CtCycles[i] = 0;
                    }
                    else if (value > (CtlineVal[i] - BaselineVal[i]) * scaleFactor[i] )//bigger than baseline that is calculated ( subtracted from ct line and multiplied by deviation )
                    {
                        Plotter.CtCycles[i] = j; // start at 3 cycles
                        tempCt = j;
                        if(Plotter.CtCycles[i] > baseCalculationMaxCycle) break; //  if CT line timing is earier than baseline Calculation period, we must pass that. 
                        else { Plotter.CtCycles[i] = 0; }
                    }
                   
                }

                //if (tempCt < 10)
                //{

                //}

                if( tempCt > baseCalculationMaxCycle)
                {
                    double tempCtline = Convert.ToDouble(MEASURED_DATA[i, (int)tempCt - 1]);
                    double delta = Math.Abs(((Convert.ToDouble(MEASURED_DATA[i, (int)tempCt]) - tempCtline)) / 100);
                    //delta *= scaleFactor[i];
                    Plotter.CtCycles[i] = tempCt;
                    for (int k = 0; k < 100; k++)
                    {
                        Plotter.CtCycles[i] += 0.01;
                        if (((tempCtline - BaselineVal[i]) * scaleFactor[i] + (delta * k)) >= (CtlineVal[i] - BaselineVal[i]) * scaleFactor[i])
                        {
                            Plotter.CtCycles[i] += scaleFactor[i];
                            break;
                        }
                        //else
                        //{
                        //    if (k == 99) Plotter.CtCycles[i] = 0;
                        //}
                    }

                   
                    {
                        Plotter.CtCycles[i] += 1;
                    }
                }

            }

            return Plotter.CtCycles;
        }

        public void baseCalculation()
        {
            int temp;
            for (int i = 0; i < 16; i++)
            {
                string ct_temp = dgv_interpretation_ct.Rows[i].Cells[1].FormattedValue.ToString();

                double iCt_temp;
                iCt_temp = Convert.ToDouble(ct_temp);
                //Int32.TryParse(ct_temp, out iCt_temp);
                //for (int j = 0; j < Plotter.COL_CNT; j++)
                for (int j = 0; j < Plotter.CycleCntForBaseCal; j++)
                {
                    double data_temp;
                    data_temp = Convert.ToDouble(MEASURED_DATA[i, j]);
                    //Int32.TryParse(MEASURED_DATA[i, j], out temp);
                    BaselineVal[i] += data_temp;
                }
                BaselineVal[i] = BaselineVal[i] / Plotter.CycleCntForBaseCal;
                //Int32.TryParse(MEASURED_DATA[i, 0], out ic_value[i]);
                CtlineVal[i] = (double)(iCt_temp * BaselineVal[i]);
                //ic_value[i] = MEASURED_DATA[i, 0];
            }

            /*
            for (int i = 0; i < 16; i++)
            {
                Int32.TryParse(MEASURED_DATA[i, 0], out ic_value[i]);
                string ct_temp = dgv_diagnosis_ct.Rows[i].Cells[1].FormattedValue.ToString();
                int iCt_temp = 0;
                Int32.TryParse(ct_temp, out iCt_temp);

                ic_value[i] = (int)(iCt_temp * ic_value[i]);
                //ic_value[i] = MEASURED_DATA[i, 0];
            }
            */
        }

        public void updateDataNGraph()
        {

            if(checkBox3.Checked)
            {
                MatchAndFindOpticDataForResult();
            }
            else
            {
                MatchAndFindOpticDataForResultForRealTime();
            }

            if (sm.DataUpdateFlag)
            {
                sm.DataUpdateFlag = false;
                sm.pre_routine_cnt = sm.Routine_Cnt;
                Plotter.ResetAllPlots(formsPlot1, formsPlot2, formsPlot3, formsPlot4);
                sm.opticReceivedFlag = true;
            }

            if (sm.opticReceivedFlag)
            {
                sm.opticReceivedFlag = false;
                DataGridView[] dgvArr = new DataGridView[4] { dgv_opticDatum_tube1, dgv_opticDatum_tube2, dgv_opticDatum_tube3, dgv_opticDatum_tube4 };

                removeAlldgv();

                sm.isFirstUpdate = false;
                FindCyclesForBaseCalculation();

                if( sm.Routine_Cnt > Plotter.MaxCycleForBaseCal) //routine_cnt)
                {

                    if (chkbox_use_avg.Checked)
                    {
                        baseCalculationAverage();//baseCalculation();

                    }
                    else if (chkbox_use_standard_dev.Checked)
                    {
                        baseCalculationAverage();
                        baseCalculationDeviation();
                    }

                    //scale factor calculation
                    scaleFactorCalculation();

                    CtCyclesCalculation();

                }

                drawDataGridView(dgvCtTable1, Plotter.CtCycles[0], Plotter.CtCycles[1], Plotter.CtCycles[2], Plotter.CtCycles[3]);
                drawDataGridView(dgvCtTable2, Plotter.CtCycles[4], Plotter.CtCycles[5], Plotter.CtCycles[6], Plotter.CtCycles[7]);
                drawDataGridView(dgvCtTable3, Plotter.CtCycles[8], Plotter.CtCycles[9], Plotter.CtCycles[10], Plotter.CtCycles[11]);
                drawDataGridView(dgvCtTable4, Plotter.CtCycles[12], Plotter.CtCycles[13], Plotter.CtCycles[14], Plotter.CtCycles[15]);

                setCtValueToDGV(dgv_interpretation_ct,Plotter.CtCycles);//setBaseValueToDataGridView(dgv_interpretation_ct, CtlineVal);
                setCtResultToDGV(dgv_interpretation_ct);
                //updateDataGridOpticDatum(dgvArr, DISPLAY_DATA); 
                updateDataGridOpticDatas(dgvArr, MEASURED_DATA);

                updateAllPlots();
                /*
                Plotter.ResetAllPlots(formsPlot1, formsPlot2, formsPlot3, formsPlot4);
                for (int i = 0; i < Plotter.CH_CNT; i++)
                {
                    if (chkBox_baselineNoScale.Checked || chkBox_BaselineScale.Checked)
                    {
                        for (int j = 0; j < Plotter.CH_CNT * Plotter.DYE_CNT; j++)
                        {
                            zerosetValArray[j] = (CtlineVal[j] - BaselineVal[j]) * scaleFactor[j];
                        }
                        updateScottPlot(i, zerosetValArray);
                    }
                    else
                    {
                        updateScottPlot(i, CtlineVal);
                    }
                }
                */

            }

            /*
            if(sm.opticReceivedFlag)
            {
                sm.opticReceivedFlag = false;

                DataGridView[] dgvArr = { dataGridView1_Engineer, dataGridView2_Engineer, dataGridView3_Engineer, dataGridView4_Engineer };
                DataGridView[] dgvArr2 = { dataGridView1_Tester, dataGridView2_Tester, dataGridView3_Tester, dataGridView4_Tester };
                DataGridView[] dgvArrCt = { dataGridView5, dataGridView6, dataGridView7, dataGridView8 };

                for (int ch_idx = 0; ch_idx < CH_CNT; ch_idx++) //CH_CNT = 4
                {
                    int idx = ch_idx * 4;
                    // 챔버인덱스(1~4), 컬럼인덱스(1~8), 데이터4개
                    // 각 사이클 데이터 입력시
                    /*
                    string[] result = CriticalThresholdCalculation(getDataGridArray(dgvArrCt[ch_idx], 3, 28));
                    lb_IC_Result.Text = result[0] + "* Value : " + result[1];
                    int tempValue = 0;  Int32.TryParse(result[1], out tempValue);
                    
                    int[] ic_value = new int[16];
                    for(int i = 0; i < 16; i++)
                    {
                        Int32.TryParse(MEASURED_DATA[i, 0], out ic_value[i]);
                        ic_value[i] = (int)(1.2 * ic_value[i]);
                        //ic_value[i] = MEASURED_DATA[i, 0];
                    }

                    for (col_Idx = 0; col_Idx < COL_CNT; col_Idx++)
                    {
                        setDataGrid_Ct(dgvArrCt[ch_idx], ch_idx + 1, col_Idx, MEASURED_DATA[idx, col_Idx], MEASURED_DATA[idx + 1, col_Idx], MEASURED_DATA[idx + 2, col_Idx], MEASURED_DATA[idx + 3, col_Idx], ic_value);
                    }

                    //setDataGrid_Ct(dgvArrCt[ch_idx], ch_idx + 1, col_idx + 1, ALL_DATA[idx, col_idx], ALL_DATA[idx + 1, col_idx], ALL_DATA[idx + 2, col_idx], ALL_DATA[idx + 3, col_idx]);
                    // setDataGrid_Engineer(dgvArr[ch_idx], ch_idx + 1, col_idx + 1, ALL_DATA[idx, col_idx], ALL_DATA[idx+1, col_idx], ALL_DATA[idx+2, col_idx], ALL_DATA[idx+3, col_idx]);
                    //setDataGrid_Engineer(dgvArr[ch_idx], ch_idx + 1, col_idx + 1, ALL_DATA[idx, col_idx], ALL_DATA[idx + 1, col_idx], ALL_DATA[idx + 2, col_idx], ALL_DATA[idx + 3, col_idx]);
                }
        
            }*/

        }


        public void initDiagnosisCT()
        {
            dgv_interpretation_ct.ColumnCount = 5;
            dgv_interpretation_ct.ColumnHeadersVisible = true;

            //DataGridViewCellStyle columnHeaderStyle = new DataGridViewCellStyle();

            //columnHeaderStyle.BackColor = Color.Beige;
            //columnHeaderStyle.Font = new Font("Verdana", 10, FontStyle.Bold);
            //dgv_diagnosis_ct.ColumnHeadersDefaultCellStyle = columnHeaderStyle;

            //set the column header names.
            dgv_interpretation_ct.Columns[0].Name = "Dye(tube)";
            dgv_interpretation_ct.Columns[1].Name = "Threshold";
            dgv_interpretation_ct.Columns[2].Name = "Result";
            dgv_interpretation_ct.Columns[3].Name = "Bases";
            dgv_interpretation_ct.Columns[4].Name = "Ct limit";
            //dataGridView_Diagnosis.Columns[9].Name = "INH";

            //dataGridView1.Rows[0].DefaultCellStyle.BackColor = Color.AliceBlue;

            //populate the rows
            //string[] sigmaScale = SigmaScaleLoad();

            string[] row1 = new string[] { "FAM1", "10", "", "","" };
            string[] row2 = new string[] { "ROX1", "10", "", "","" };
            string[] row3 = new string[] { "HEX1", "10", "", "" ,""};
            string[] row4 = new string[] { "CY51", "10", "", "" ,""};

            string[] row5 = new string[] { "FAM2", "10", "", "" ,""};
            string[] row6 = new string[] { "ROX2", "10", "", "" ,""};
            string[] row7 = new string[] { "HEX2", "10", "", "" ,""};
            string[] row8 = new string[] { "CY52", "10", "", "" ,""};

            string[] row9 = new string[] { "FAM3", "10", "", "" ,""};
            string[] row10 = new string[] { "ROX3", "10", "", "" ,""};
            string[] row11 = new string[] { "HEX3", "10", "", "" ,""};
            string[] row12 = new string[] { "CY53", "10", "", "" ,""};

            string[] row13 = new string[] { "FAM4", "10", "", "" ,""};
            string[] row14 = new string[] { "ROX4", "10", "", "" ,""};
            string[] row15 = new string[] { "HEX4", "10", "", "" ,""};
            string[] row16 = new string[] { "CY54", "10", "", "" ,""};

            object[] rows = new object[] { row1, row2, row3, row4, row5, row6, row7, row8, row9, row10, row11, row12, row13, row14, row15, row16 };

            foreach (string[] rowArray in rows)
            {
                dgv_interpretation_ct.Rows.Add(rowArray);
            }

        }

        //excel libraries
      
        ////////////////////////////////////


        private void setDataGrid_diagnosis(DataGridView dgv, string[,] value, int x_count, int y_count)
        {
            //string[] data2 = new string[10] { value, "0", "0", "0", "0", "0", "0", "0", "0", "0" };
            //string[] data3 = new string[10] { value, "0", "0", "0", "0", "0", "0", "0", "0", "0" };
            //string[] data4 = new string[10] { value, "0", "0", "0", "0", "0", "0", "0", "0", "0" };
            //string[] data5 = new string[10] { value, "0", "0", "0", "0", "0", "0", "0", "0", "0" };

            for (int j = 0; j < x_count; j++)
            {
                string[] temp = new string[y_count];

                for (int i = 0; i < y_count; i++)
                {
                    temp[i] = value[j, i];

                }
                dgv.Rows.Add(temp);
            }
            //dgv.Rows.Add(data2);
            //dgv.Rows.Add(data3);
            //dgv.Rows.Add(data4);

            dgv.Font = new Font("문체부 제목 돋음체", 18, FontStyle.Regular);
            dgv.DefaultCellStyle.Font = new Font("문체부 제목 돋음체", 18, FontStyle.Bold);

            // 그리드뷰를 설정
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (i % 2 != 0)
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.White;
                }
                else
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                }
            }
        }


        public void drawDataGridView(DataGridView dgv, double one, double two, double three, double four)
        {
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dgv.GridColor = Color.Black;
            dgv.DefaultCellStyle.Font = new Font("Arial", 12.5F, GraphicsUnit.Pixel);
            dgv.EditMode = DataGridViewEditMode.EditProgrammatically;
            /*
            string[] row0 = { "FAM", Plotter.CtCycles[0].ToString() };
            string[] row1 = { "ROX", Plotter.CtCycles[0].ToString() };
            string[] row2 = { "HEX", Plotter.CtCycles[0].ToString() };
            string[] row3 = { "CY5", Plotter.CtCycles[0].ToString() };

            dgvCtTable1.Rows.Add(row0);
            dgvCtTable1.Rows.Add(row1);
            dgvCtTable1.Rows.Add(row2);
            dgvCtTable1.Rows.Add(row3);

            dgvCtTable1.Columns[0].DisplayIndex = 0;
            dgvCtTable1.Columns[1].DisplayIndex = 1;
            */

            DataTable dt = new DataTable();
            dt.Columns.Add("Dye", typeof(string));
            dt.Columns.Add("Ct", typeof(string));

            double val1 = Math.Truncate(one * 100) / 100;
            double val2 = Math.Truncate(two * 100) / 100;
            double val3 = Math.Truncate(three * 100) / 100;
            double val4 = Math.Truncate(four * 100) / 100;

            dt.Rows.Add("FAM", val1.ToString());
            dt.Rows.Add("ROX", val2.ToString());
            dt.Rows.Add("HEX", val3.ToString());
            dt.Rows.Add("CY5", val4.ToString());

            dgv.DataSource = dt;

        }



        


        bool IsTheSameCellValue(int column, int row)
        {
            DataGridViewCell cell1 = dgv_interpretation_howTo[column, row];
            DataGridViewCell cell2 = dgv_interpretation_howTo[column, row - 1];
            if (cell1.Value == null || cell2.Value == null)
            {
                return false;
            }
            return cell1.Value.ToString() == cell2.Value.ToString();
        }



        private void DataGridView1_CellFormatting1(object sender, System.Windows.Forms.DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex == 0)
                return;
            if (IsTheSameCellValue(e.ColumnIndex, e.RowIndex))
            {
                e.Value = "";
                e.FormattingApplied = true;
            }
        }

        private void DataGridView1_CellPainting1(object sender, System.Windows.Forms.DataGridViewCellPaintingEventArgs e)
        {
            e.AdvancedBorderStyle.Bottom = DataGridViewAdvancedCellBorderStyle.None;
            if (e.RowIndex < 1 || e.ColumnIndex < 1)
                return;
            if (IsTheSameCellValue(e.ColumnIndex, e.RowIndex))
            {
                e.AdvancedBorderStyle.Top = DataGridViewAdvancedCellBorderStyle.None;
            }
            else
            {
                e.AdvancedBorderStyle.Top = dgv_interpretation_howTo.AdvancedCellBorderStyle.Top;
            }
        }

        private void setDiagnosisTable()
        {
            dgv_interpretation_howTo.ColumnCount = 22;
            dgv_interpretation_howTo.ColumnHeadersVisible = true;

            DataGridViewCellStyle columnHeaderStyle = new DataGridViewCellStyle();

            columnHeaderStyle.BackColor = Color.Beige;
            columnHeaderStyle.Font = new Font("Verdana", 10, FontStyle.Bold);
            dgv_interpretation_howTo.ColumnHeadersDefaultCellStyle = columnHeaderStyle;

            //set the column header names.
            dgv_interpretation_howTo.Columns[0].Name = "Dye(tube)";
            dgv_interpretation_howTo.Columns[1].Name = "TB";
            dgv_interpretation_howTo.Columns[2].Name = "NTM";
            dgv_interpretation_howTo.Columns[3].Name = "NEG";
            dgv_interpretation_howTo.Columns[4].Name = "RIF";
            dgv_interpretation_howTo.Columns[5].Name = "RIF";
            dgv_interpretation_howTo.Columns[6].Name = "RIF";
            dgv_interpretation_howTo.Columns[7].Name = "RIF";
            dgv_interpretation_howTo.Columns[8].Name = "RIF";
            dgv_interpretation_howTo.Columns[9].Name = "RIF";
            dgv_interpretation_howTo.Columns[10].Name = "RIF";
            //dgv_interpretation_howTo.Columns[11].Name = "Dye(tube)";
            //dgv_interpretation_howTo.Columns[12].Name = "TB";
            //dgv_interpretation_howTo.Columns[13].Name = "NTM";
            //dgv_interpretation_howTo.Columns[14].Name = "NEG";
            //dgv_interpretation_howTo.Columns[15].Name = "RIF";
            //dgv_interpretation_howTo.Columns[16].Name = "RIF";
            //dgv_interpretation_howTo.Columns[17].Name = "RIF";
            //dgv_interpretation_howTo.Columns[18].Name = "RIF";
            //dgv_interpretation_howTo.Columns[19].Name = "RIF";
            //dgv_interpretation_howTo.Columns[20].Name = "RIF";
            //dgv_interpretation_howTo.Columns[21].Name = "RIF";


            //dataGridView1.Rows[0].DefaultCellStyle.BackColor = Color.AliceBlue;

            //populate the rows
            string[] row1 = new string[] { "FAM(Tube 1)", "P", "N", "N", "", "", "", "", "", "", "P", "N", "N", "", "", "", "", "", "" };
            string[] row2 = new string[] { "ROX(Tube 1)", "", "", "", "", "", "", "", "", "", "P", "N", "N", "", "", "", "", "", "" };
            string[] row3 = new string[] { "HEX(Tube 1)", "", "", "", "", "", "", "", "", "", "P", "N", "N", "", "", "", "", "", "" };
            string[] row4 = new string[] { "Cy5(Tube 1)", "P", "P", "P", "", "", "", "", "", "", "P", "N", "N", "", "", "", "", "", "" };
            string[] row5 = new string[] { "FAM(Tube 2)", "P", "P", "N", "", "", "", "", "", "", "P", "N", "N", "", "", "", "", "", "" };
            string[] row6 = new string[] { "ROX(Tube 2)", "", "", "", "P", "", "P", "P", "", "", "P", "N", "N", "", "", "", "", "", "" };
            string[] row7 = new string[] { "HEX(Tube 2)", "", "", "", "", "", "", "", "", "", "P", "N", "N", "", "", "", "", "", "" };
            string[] row8 = new string[] { "Cy5(Tube 2)", "", "", "", "", "", "", "", "", "", "P", "N", "N", "", "", "", "", "", "" };
            string[] row9 = new string[] { "FAM(Tube 3)", "", "", "", "P", "P", "", "P", "", "", "P", "N", "N", "", "", "", "", "", "" };
            string[] row10 = new string[] { "ROX(Tube 3)", "", "", "", "P", "P", "P", "P", "", "", "P", "N", "N", "", "", "", "", "", "" };
            string[] row11 = new string[] { "ROX(Tube 3)", "", "", "", "", "", "", "", "", "", "P", "N", "N", "", "", "", "", "", "" };
            string[] row12 = new string[] { "ROX(Tube 3)", "", "", "", "", "", "", "", "", "", "P", "N", "N", "", "", "", "", "", "" };
            string[] row13 = new string[] { "FAM(Tube 4)", "", "", "", "P", "P", "P", "", "", "", "P", "N", "N", "", "", "", "", "", "" };
            string[] row14 = new string[] { "ROX(Tube 4)", "", "", "", "", "", "", "", "", "", "P", "N", "N", "", "", "", "", "", "" };
            string[] row15 = new string[] { "HEX(Tube 4)", "", "", "", "", "", "", "", "", "", "P", "N", "N", "", "", "", "", "", "" };
            string[] row16 = new string[] { "Cy5(Tube 4)", "", "", "", "", "P", "P", "P", "", "", "P", "N", "N", "", "", "", "", "", "" };

            object[] rows = new object[] { row1, row2, row3, row4, row5, row6, row7, row8, row9, row10, row11, row12, row13, row14, row15, row16 };

            foreach (string[] rowArray in rows)
            {
                dgv_interpretation_howTo.Rows.Add(rowArray);
            }

            /*
            DataTable table = new DataTable();

            // column을 추가합니다.
            table.Columns.Add("ID", typeof(string));
            table.Columns.Add("제목", typeof(string));
            table.Columns.Add("구분", typeof(string));
            table.Columns.Add("생성일", typeof(string));
            table.Columns.Add("수정일", typeof(string));

            // 각각의 행에 내용을 입력합니다.
            table.Rows.Add("ID 1", "제목 1번", "사용중", "2019/03/11", "2019/03/18");
            table.Rows.Add("ID 2", "제목 2번", "미사용", "2019/03/12", "2019/03/18");
            table.Rows.Add("ID 3", "제목 3번", "미사용", "2019/03/13", "2019/03/18");
            table.Rows.Add("ID 4", "제목 4번", "사용중", "2019/03/14", "2019/03/18");

            // 값들이 입력된 테이블을 DataGridView에 입력합니다.
            dataGridView1.DataSource = table;
            */
        }

        private bool Open_exec()
        {
            if (isConnected)
            {
                serial.Close();
                isConnected = false;
                //ConnectChanged(this, EventArgs.Empty);
                return false;
            }
            else
            {
                bool success = false;

                if ((serial != null) && (serial.IsConnect() == false))
                {
                    bool tryConnect = false;

                    try
                    {
                        tryConnect = serial.Connect(cb_Port_Main.SelectedItem.ToString());
                    }
                    catch
                    { }

                    if (tryConnect)
                    {
                        if (true)
                        {
                            //Open_cmd.CanSet = false;
                            isConnected = true;
                            success = true;
                            return true;
                        }
                    }
                }

                if (success != true)
                {

                }
                else
                {

                }

                return false;
            }
        }

        private bool Open_exec_barcode()
        {
            if (isConnected)
            {
                serialBarcode.Close();
                isConnected = false;
                //ConnectChanged(this, EventArgs.Empty);
                return false;
            }
            else
            {
                bool success = false;

                if ((serialBarcode != null) && (serialBarcode.IsConnect() == false))
                {
                    bool tryConnect = false;

                    try
                    {
                        tryConnect = serialBarcode.Connect("COM4"); //cb_Port_Main.SelectedItem.ToString()
                    }
                    catch
                    { }

                    if (tryConnect)
                    {
                        if (true)
                        {
                            //Open_cmd.CanSet = false;
                            isConnected = true;
                            success = true;
                            return true;
                        }
                    }
                }

                if (success != true)
                {

                }
                else
                {

                }

                return false;
            }
        }

        private void SetRecipeCombobox_Test()
        {
            cb_Recipe_Eng.Items.Clear();
            cb_Recipe_Test.Items.Clear();

            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");

            foreach (System.IO.FileInfo File in di.GetFiles())
            {
                if (File.Extension.ToLower().CompareTo(".rcp") == 0)
                {
                    String FileNameOnly = File.Name.Substring(0, File.Name.Length - 4);
                    String FullFileName = File.FullName;

                    //MessageBox.Show(FullFileName + " " + FileNameOnly);
                    cb_Recipe_Eng.Items.Add(FileNameOnly);
                    cb_Recipe_Test.Items.Add(FileNameOnly);
                }
            }
        }
        public void setAccessibility()
        {
            if (sm.userAccessibility == "tester")
            {
                setUserMode(1);
            }
            else if (sm.userAccessibility == "administrator")
            {
                setUserMode(2);
            }
            else if (sm.userAccessibility == "engineer")
            {
                setUserMode(3);
            }


        }


        private void btn_Connect_Main_Click(object sender, EventArgs e)
        {
            // 컴포트 선택 후 연결 버튼 클릭하면 ID, PW 입력창이 활성화됨
            //showLog("장치와 연결 동작");

            //컴포트 연결시도
            bool flag = Open_exec();
            //bool flag = true; //Plot Debug



            tb_Log.Visible = true;

            //연결에 성공하면
            if (flag)
            {
                tb_ID_Main.Enabled = true;
                tb_PW_Main.Enabled = true;
                btn_Login_Main.Enabled = true;
                btn_Connect_Main.Text = "Disconnect";
            }
            else
            {
                tb_ID_Main.Enabled = false;
                tb_PW_Main.Enabled = false;
                btn_Login_Main.Enabled = false;
                btn_Connect_Main.Text = "Connect";
            }
        }
        private void btn_Login_Main_Click(object sender, EventArgs e)
        {
            if (tb_ID_Main.Text == "ABI" && tb_PW_Main.Text == "5344")
            {
                // 엔지니어 계정 로그인임 --> 계정정보에서도 관리 가능
                setUserMode(3);
                tb_Log.Visible = true;

                tb_WorkerID_Test.Text = "ABI";
                tb_WorkerName_Test.Text = "ABI엔지니어";

                // tb_ID_Main.Text = "";
                tb_PW_Main.Text = "";
                return;
            }
            if (btn_Login_Main.Text == "LOG OUT")
            {
                setUserMode(0);
                btn_Login_Main.Text = "LOGIN";
                MessageBox.Show("로그아웃 했습니다.", "로그아웃 안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                tabControl1.SelectedIndex = 0;

                tb_ID_Main.Text = "";
                tb_PW_Main.Text = "";

                return;
            }

            // 로그인 계정을 체크
            string userRight = "NOT";

            // 계정에서 로그인한 계정 유무를 확인함
            // 데이터 비교
            foreach (DataGridViewRow row in dataGridView_Manage.Rows)
            {
                if (!row.IsNewRow)
                {
                    for (int i = 0; i < dataGridView_Manage.Columns.Count; i++)
                    {
                        if (tb_ID_Main.Text == row.Cells[1].Value.ToString() // ID
                            && tb_PW_Main.Text == row.Cells[2].Value.ToString()) // PW
                        {
                            userRight = row.Cells[3].Value.ToString();
                            tb_WorkerID_Test.Text = tb_ID_Main.Text;
                            tb_WorkerName_Test.Text = row.Cells[0].Value.ToString();
                        }
                    }
                }
            }

            switch (userRight)
            {
                case "tester":
                    //MessageBox.Show("검사자 계정으로 로그인 했습니다.", "로그인 안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    setUserMode(1);
                    break;
                case "administrator":
                    //MessageBox.Show("관리자 계정으로 로그인 했습니다.", "로그인 안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    setUserMode(2);
                    break;
                case "engineer":
                    //MessageBox.Show("엔지니어 계정으로 로그인 했습니다.", "로그인 안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    setUserMode(3);
                    break;
                case "NOT":
                    MessageBox.Show("계정 정보 또는 비밀번호가 틀립니다. 관리자에게 문의 바랍니다.", "로그인 안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    setUserMode(0);
                    break;
            }
        }
        private void tabControl1_Selected(object sender, TabControlEventArgs e)
        {
            // 탭의 이동을 원할 경우 권한을 체크함
            if (userMode == 0)
            {
                tabControl1.SelectedIndex = 0;
            }
            else if (userMode == 1)
            {
                if (tabControl1.SelectedIndex == 1 || tabControl1.SelectedIndex == 2)
                {
                    tabControl1.SelectedIndex = 0;
                }
            }
            else if (userMode == 2)
            {
                if (tabControl1.SelectedIndex == 2)
                {
                    tabControl1.SelectedIndex = 1;
                }
            }

            btn_Search_IDManage_Click(this, null);
        }
        private void tabControl4_Selected(object sender, TabControlEventArgs e)
        {
            // 탭의 이동을 원할 경우 권한을 체크함
            if (userMode == 1)
            {
                tabControl_Tester.SelectedIndex = 0;   // 권한이 없는 검사자는 관리자탭으로 이동 불가
            }
            if (processStep == 3) // 검사를 시작한 경우라면 관리자도 탭을 이동할 수 없음
            {
                // 관리자가 현재 보고 있는 탭을 체크하여 그대로 유지하여야 함
                // todo
            }
        }



        // 검사 준비 ==> 관련 초기화 실시
        private void btn_OpenDoor_Test_Click(object sender, EventArgs e)
        {
            sm.pre_routine_cnt = 0;

            updateDeviceStatus("Getting Ready");
            //all about reset
            updateTestInfo();
            resetTesterInfo();
            resetCartridgeInfo();

            initAllScottPlot();
            removeAlldgv();
                      
            logToFile.MakeNewFile();


            presetDataGrid_ct(dgv_opticDatum_tube1, chamberName[0]);
            presetDataGrid_ct(dgv_opticDatum_tube2, chamberName[1]);
            presetDataGrid_ct(dgv_opticDatum_tube3, chamberName[2]);
            presetDataGrid_ct(dgv_opticDatum_tube4, chamberName[3]);

            // MCU에 문을 열라고 명령함
            if (!isConnected) {
                // 검사 장비와 연결이 되어야 진행함
                MessageBox.Show("먼저 검사 장비와 연결이 되야 합니다. 연결해 주세요.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 데이터 버퍼 초기화
            for (int i = 0; i < Plotter.CH_CNT * 4; i++)
            {
                CH_BASE_DATA[i] = "";
                for (int j = 0; j < Plotter.COL_CNT; j++)
                {
                    ALL_DATA[i, j] = "";
                }
            }

            // 그리드 표시값을 삭제
            //btnSetBaseData_Click(null, null);
            //btnSetFinalData_Click(null, null);

            setProcessMode(0);

            StringBuilder sb = new StringBuilder();
            sb.Append(":DOOR_UNLOCK");
            String txt = sb.ToString();
            //if (checkBox1.Checked) 
            serial.SendLine(txt);

            // 문의 락이 풀리면 --> MCU에서 문이 열리는 것을 신호를 받은 후 메시지 출력?
            if (MessageBox.Show("문 상단을 눌러 열어 주세요.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information) == DialogResult.OK)
            {// 예 클릭

                setProcessMode(1);
                if (MessageBox.Show("환자 ID, 샘플 ID 및 카트리지 ID 입력 후 검사설정을 해 주세요.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information) == DialogResult.OK)
                {
                    // setProcessMode(2);
                    // 샘플 ID와 카트리지 ID 입력 가능 상태로 전환 --> 포커스를 옮김
                    setProcessMode(3);
                    tb_SampleID_Test.Focus();

                }
                else   // 취소
                {
                    //처음으로 돌아감
                    MessageBox.Show("작업 취소로 문을 닫습니다.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    setProcessMode(0);
                }

            }
            else
            { // 취소 클릭
              //처음으로 돌아감
                MessageBox.Show("작업 취소로 문을 닫습니다", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                setProcessMode(0);
            }

            if (processStep > 2)
            {
                gb_CartridgeInfo.Enabled = true;
                gb_TestInfo.Enabled = true;
            }


            /*
             AbortRetryIgnore : 중단, 다시시도, 무시 버튼 표시
        OK : 확인 버튼 표시 
        OKCancel : 확인, 취소 버튼 표시 
        RetryCancel : 다시 시도, 취소 버튼 표시 
        YesNo : 예, 아니요 버튼 표시 
        YesNoCancel : 예, 아니요, 취소 버튼 표시             

        Asterisk : 정보 아이콘 
        Error : 빨간색 원 안 X 표시 
        Exclamation : 경고 아이콘 
        Hand : 손바닥 아이콘 
        Information : 정보 아이콘
        None : 아이콘을 표시하지 않음 
        Question : 물음표 아이콘 
        Stop : 빨간색 원 안 X 표시 
        Warning : 경고 아이콘
             */

        }
        private void btn_SetRecipe_Test_Click(object sender, EventArgs e)
        {
            updateTesterInfo();
            updateCartridgeInfo();

            if (cb_Recipe_Test.SelectedIndex == -1)
            {
                MessageBox.Show("검사 항목을 선택 후 확인을 눌러주세요.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            else if (processStep != 4)
            {
                MessageBox.Show("환자 ID, 샘플 ID 및 카트리지 ID 입력이 끝나야 검사설정을 할 수 있습니다.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            else if (processStep == 4)
            {
                // MCU 설정을 위해 레시피 정보 공유
                cb_Recipe_Eng.SelectedIndex = cb_Recipe_Test.SelectedIndex;  // 인덱스를 동일하게
                btn_LoadRecipe_Eng_Click(null, null); // 레시피를 불러옴
                Apply_exec(null, null);  // MCU에 전체 전송

                if (DialogResult.OK == MessageBox.Show("카트리지 커버 및 캡을 제거해 주세요.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information))
                {
                    setProcessMode(5);

                    aboutBox.ShowDialog();

                    if (aboutBox.retBtn == 1)
                    {
                        // 확인 --> 계속 진행
                        setProcessMode(6);
                        setProcessMode(7);
                        //if (DialogResult.OK == MessageBox.Show("팁을 장착해 주세요.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information))
                        //{
                        if (DialogResult.OK == MessageBox.Show("준비가 끝났습니다. 문을 닫고 [검사 시작]을 눌러 검사를 진행해 주세요.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information))
                        {
                            setProcessMode(8);
                        }
                        //}
                    }
                    else
                    {
                        // 취소 --> 작업 취소
                        MessageBox.Show("검사 취소함", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        setProcessMode(0);
                    }

                }
            }
        }


        private void btn_Start_Test_Click(object sender, EventArgs e)
        {
            //update test infomation
            
            //updatePatientInfo();

            /*
            listView_ReportInfo.BeginUpdate();

            lvi1 = new ListViewItem("MTBC");
            listView_ReportInfo.Items[0].SubItems[2].Text = " dahunj";
            //lvi1.SubItems.Add(test_start_time);
            //lvi1.SubItems.Add("");

            //lvi1.SubItems[0].BackColor = Color.WhiteSmoke;

            //lvi1.ImageIndex = 0;
            listView_ReportInfo.Items.Add(lvi1);

            listView_ReportInfo.EndUpdate();
            */

            if (processStep == 8)  // ID input complete
            {
                MessageBox.Show("검사를 시작할 수 있나 체크합니다.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                updateDeviceStatus("Running");
                serial.SendLine(":check_start");  // 문이 열렸는지 체크 의뢰 
            }
            else if (processStep == 9)
            {
                if (DialogResult.OK == MessageBox.Show("검사가 진행 중입니다. 검사를 멈추겠습니까?", "검사 안내", MessageBoxButtons.OKCancel, MessageBoxIcon.Information))
                {
                    _reset_Process();
                    
                    MessageBox.Show("검사를 강제 종료하였습니다.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("검사 준비가 되지 않았습니다.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void _reset_Process()
        {
            //log data 
            allData = "";
            allDataArray = new string[] { };

            // MCU 장비 초기화
            serial.SendLine(":stop");

            // 새로운 검사를 위한 모든 초기화를 여기에서 수행
            procData = null;
            waitData = "";

            //sm.logLineCnt = 0;

            // 데이터 받을 준비되었음
            routine_cnt = 0;
            sm.pre_routine_cnt = routine_cnt;

            // 검사관련 변수 초기화
            sm.measured_cnt = 0;
            sm.opticReceivedFlag = false;

            col_Idx = 0;

            // 버튼 초기화
            b_GUI_Init = true;

            logToFile.CloseFile();
            bSaveLog = false;

            setProcessMode(0);

            routine_cnt = 0;
            processStep = 0;

            sm.COVID_RESULT1 = new string[3];
            sm.COVID_RESULT2 = new string[3];
            sm.COVID_RESULT3 = new string[3];
            sm.COVID_RESULT4 = new string[3];

        }
        private void _start_Process()
        {
            sm.isFirstUpdate = true;

            btn_Start_Test.Text = "Shutdown";//"강제 종료";
            setProcessMode(9);

            bSaveLog = true;
           

            // 시작 명령 전송
            serial.SendLine(":start");

            // 진행바 초기화
            setProgressInfo(-1);

            MessageBox.Show("검사를 시작합니다.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void _check_Door()
        {
            MessageBox.Show("문이 열려 있습니다. 문을 닫고 [검사 시작]을 다시 눌러 주세요.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public int id_selectedIndex { get; set; }

        enum ID_INDEX {
            PATIENT_ID = 0,
            SAMPLE_ID,
            CARTRIDGE_ID
        }

        private void tb_PatientID_Test_TextChanged(object sender, EventArgs e)
        {
            var triggerOffCmd = new byte[] { 0x1b, 0x41, 0x31, 0x0d };
            serialBarcode.SendBin(triggerOffCmd);

            if (processStep == 3 && tb_PatientID_Test.Text != "" && tb_CartridgeID_Test.Text != "" && tb_SampleID_Test.Text != "")
            {
                //MessageBox.Show("다음으로 진행함", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                setProcessMode(4);
            }
        }



        private void tb_SampleID_Test_TextChanged(object sender, EventArgs e)
        {
            var triggerOffCmd = new byte[] { 0x1b, 0x41, 0x31, 0x0d };
            serialBarcode.SendBin(triggerOffCmd);


            if (processStep == 3 && tb_PatientID_Test.Text != "" && tb_CartridgeID_Test.Text != "" && tb_SampleID_Test.Text != "")
            {
                //MessageBox.Show("다음으로 진행함", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                setProcessMode(4);
            }
        }

        private void tb_CartridgeID_Test_TextChanged(object sender, EventArgs e)
        {
            var triggerOffCmd = new byte[] { 0x1b, 0x41, 0x31, 0x0d };
            serialBarcode.SendBin(triggerOffCmd);

            if (processStep == 3 && tb_PatientID_Test.Text != "" && tb_CartridgeID_Test.Text != "" && tb_SampleID_Test.Text != "")
            {
                //MessageBox.Show("다음으로 진행함", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                setProcessMode(4);
            }
        }

        private void tb_PatientID_Test_CursorChanged(object sender, EventArgs e)
        {
            var triggerCmd = new byte[] { 0x1b, 0x41, 0x32, 0x0d };
            serialBarcode.SendBin(triggerCmd);
            id_selectedIndex = (int)ID_INDEX.PATIENT_ID;
        }

        private void tb_SampleID_Test_CursorChanged(object sender, EventArgs e)
        {
            var triggerCmd = new byte[] { 0x1b, 0x41, 0x32, 0x0d };
            serialBarcode.SendBin(triggerCmd);
            id_selectedIndex = (int)ID_INDEX.SAMPLE_ID;
        }

        private void tb_CartridgeID_Test_CursorChanged(object sender, EventArgs e)
        {
            var triggerCmd = new byte[] { 0x1b, 0x41, 0x32, 0x0d };
            serialBarcode.SendBin(triggerCmd);
            id_selectedIndex = (int)ID_INDEX.CARTRIDGE_ID;
        }

        private void btn_About_Click(object sender, EventArgs e)
        {
            aboutBox.ShowDialog();
            if (aboutBox.retBtn == 1)
            {
                // 확인 --> 계속 진행
                MessageBox.Show("다음으로 진행함", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // 취소 --> 작업 취소
                MessageBox.Show("검사 취소함", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void Form1_Shown(object sender, EventArgs e)
        {
            dataGridView1_Engineer.CurrentCell = null;
            dataGridView2_Engineer.CurrentCell = null;
            dataGridView3_Engineer.CurrentCell = null;
            dataGridView4_Engineer.CurrentCell = null;
        }



        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!backgroundWorker1.IsBusy)
            {
                _inputparameter.Delay = 0;

                backgroundWorker1.RunWorkerAsync(_inputparameter);
            }

            /*
            string cTime = DateTime.Now.ToLongTimeString();
            string cDate = DateTime.Now.ToLongDateString();
            this.Text = "ABI POC PCR ver. 2.0.0     [" + cDate + " | " + cTime + "]";

            // 데이터 버퍼가 찼는지 확인하고 업데이트
            nTimerNo++;
            if (nTimerNo % 5 == 0 && processStep == 9 && routine_cnt > -1) // 5초 마다 업데이트
            {
                nTimerNo = 0;
                btnSetBaseData_Click(null, null);
                btnSetFinalData_Click(null, null);
            }

            if (processStep == 9) // 5초 마다 업데이트
            {
                // 상태바 업데이트 - 시작을 20%에서 시작함
                progressBar_Tester.Value = (int)(30 + routine_cnt * 2);
                progressBar_Manager.Value = (int)(30 + routine_cnt * 2);
            }

            // 버퍼에 쓰레기가 있을 경우 체크해서 클리어
            if (waitData.Length != 0)
                if (waitData[0].Equals('-') || waitData[0].Equals('<') || waitData[0].Equals('>'))
                    waitData = "";

            if (waitData.Length > 100) waitData = "";


            if (b_start_Process && processStep != 9) {
                b_start_Process = false;
                _start_Process();
            }

            if (b_check_Door)
            {
                b_check_Door = false;
                _check_Door();
            }

            if (b_GUI_Init) {
                // 검사단계 초기화
                setProcessMode(0);
                setProgressInfo(-1);
                btn_Start_Test.Text = "Test Start";

                b_GUI_Init = false;
            }
            */
        }

        public void initGridValue()
        {
            //ALL_DATA[0, 0] = "26.474245632";
            //ALL_DATA[0, 1] = "26.652167154";
            //ALL_DATA[0, 2] = "26.219731428";
            //ALL_DATA[0, 3] = "27.110233116";
            //ALL_DATA[0, 4] = "27.211859982";
            //ALL_DATA[0, 5] = "26.525208078";
            //ALL_DATA[0, 6] = "199.999999999";
            //ALL_DATA[1, 0] = "26.881051122";
            //ALL_DATA[1, 1] = "25.406120448";
            //ALL_DATA[1, 2] = "24.236368398";
            //ALL_DATA[1, 3] = "25.914552804";
            //ALL_DATA[1, 4] = "25.507747314";
            //ALL_DATA[1, 5] = "25.507747314";
            //ALL_DATA[1, 6] = "299.999999999";
            //ALL_DATA[2, 0] = "18.615896064";
            //ALL_DATA[2, 1] = "19.582394382";
            //ALL_DATA[2, 2] = "18.615896064";
            //ALL_DATA[2, 3] = "18.71752293";
            //ALL_DATA[2, 4] = "18.74285514";
            //ALL_DATA[2, 5] = "18.234422784";
            //ALL_DATA[2, 6] = "399.999999999";
            //ALL_DATA[3, 0] = "13.885627392";
            //ALL_DATA[3, 1] = "14.267100672";
            //ALL_DATA[3, 2] = "14.317765092";
            //ALL_DATA[3, 3] = "13.758668316";
            //ALL_DATA[3, 4] = "13.809332736";
            //ALL_DATA[3, 5] = "13.275270144";
            //ALL_DATA[3, 6] = "499.999999999";
            //ALL_DATA[4, 0] = "26.906681358";
            //ALL_DATA[4, 1] = "26.626834944";
            //ALL_DATA[4, 2] = "26.32165632";
            //ALL_DATA[4, 3] = "26.906681358";
            //ALL_DATA[4, 4] = "26.550540288";
            //ALL_DATA[4, 5] = "26.372618766";
            //ALL_DATA[4, 6] = "599.999999999";
            //ALL_DATA[5, 0] = "26.474245632";
            //ALL_DATA[5, 1] = "26.067142116";
            //ALL_DATA[5, 2] = "25.635004416";
            //ALL_DATA[5, 3] = "26.72846181";
            //ALL_DATA[5, 4] = "25.88951862";
            //ALL_DATA[5, 5] = "26.169067008";
            //ALL_DATA[5, 6] = "699.999999999";
            //ALL_DATA[6, 0] = "20.904735744";
            //ALL_DATA[6, 1] = "21.260876814";
            //ALL_DATA[6, 2] = "19.989199872";
            //ALL_DATA[6, 3] = "20.879105508";
            //ALL_DATA[6, 4] = "21.057325056";
            //ALL_DATA[6, 5] = "20.803108878";
            //ALL_DATA[6, 6] = "799.999999999";
            //ALL_DATA[7, 0] = "14.394357774";
            //ALL_DATA[7, 1] = "13.606079004";
            //ALL_DATA[7, 2] = "12.18181275";
            //ALL_DATA[7, 3] = "13.73303808";
            //ALL_DATA[7, 4] = "13.7837025";
            //ALL_DATA[7, 5] = "13.961922048";
            //ALL_DATA[7, 6] = "899.999999999";
            //ALL_DATA[8, 0] = "139.26307941";
            //ALL_DATA[8, 1] = "138.14399178";
            //ALL_DATA[8, 2] = "138.83094171";
            //ALL_DATA[8, 3] = "137.940738048";
            //ALL_DATA[8, 4] = "138.44946843";
            //ALL_DATA[8, 5] = "137.533932558";
            //ALL_DATA[8, 6] = "999.999999999";
            //ALL_DATA[9, 0] = "28.22902272";
            //ALL_DATA[9, 1] = "28.483536924";
            //ALL_DATA[9, 2] = "28.457906688";
            //ALL_DATA[9, 3] = "29.500699662";
            //ALL_DATA[9, 4] = "28.25435493";
            //ALL_DATA[9, 5] = "28.305317376";
            //ALL_DATA[9, 6] = "1099.999999999";
            //ALL_DATA[10, 0] = "30.288978432";
            //ALL_DATA[10, 1] = "30.009132018";
            //ALL_DATA[10, 2] = "29.958169572";
            //ALL_DATA[10, 3] = "29.958169572";
            //ALL_DATA[10, 4] = "29.805878286";
            //ALL_DATA[10, 5] = "29.449737216";
            //ALL_DATA[10, 6] = "1199.999999999";
            //ALL_DATA[11, 0] = "13.961922048";
            //ALL_DATA[11, 1] = "14.49598464";
            //ALL_DATA[11, 2] = "14.343395328";
            //ALL_DATA[11, 3] = "14.699536398";
            //ALL_DATA[11, 4] = "14.49598464";
            //ALL_DATA[11, 5] = "14.343395328";
            //ALL_DATA[11, 6] = "1299.999999999";
            //ALL_DATA[12, 0] = "31.30614117";
            //ALL_DATA[12, 1] = "31.229846514";
            //ALL_DATA[12, 2] = "31.916498418";
            //ALL_DATA[12, 3] = "31.30614117";
            //ALL_DATA[12, 4] = "31.713244686";
            //ALL_DATA[12, 5] = "31.179182094";
            //ALL_DATA[12, 6] = "1399.999999999";
            //ALL_DATA[13, 0] = "25.965813276";
            //ALL_DATA[13, 1] = "26.245361664";
            //ALL_DATA[13, 2] = "26.34698853";
            //ALL_DATA[13, 3] = "25.55870976";
            //ALL_DATA[13, 4] = "25.838556174";
            //ALL_DATA[13, 5] = "26.092772352";
            //ALL_DATA[13, 6] = "1499.999999999";
            //ALL_DATA[14, 0] = "19.251585522";
            //ALL_DATA[14, 1] = "19.404174834";
            //ALL_DATA[14, 2] = "18.895742478";
            //ALL_DATA[14, 3] = "19.04833179";
            //ALL_DATA[14, 4] = "20.59955712";
            //ALL_DATA[14, 5] = "18.387012096";
            //ALL_DATA[14, 6] = "1599.999999999";
            //ALL_DATA[15, 0] = "13.910959602";
            //ALL_DATA[15, 1] = "14.419689984";
            //ALL_DATA[15, 2] = "14.368727538";
            //ALL_DATA[15, 3] = "14.87745792";
            //ALL_DATA[15, 4] = "13.529486322";
            //ALL_DATA[15, 5] = "15.131972124";
            //ALL_DATA[15, 6] = "1699.999999999";

            //CH_BASE_DATA[0] = "126.34698853";
            //CH_BASE_DATA[1] = "225.55870976";
            //CH_BASE_DATA[2] = "326.34698853";
            //CH_BASE_DATA[3] = "3326.34698853";
            //CH_BASE_DATA[4] = "425.55870976";
            //CH_BASE_DATA[5] = "526.34698853";
            //CH_BASE_DATA[6] = "625.55870976";
            //CH_BASE_DATA[7] = "726.34698853";
            //CH_BASE_DATA[8] = "825.55870976";
            //CH_BASE_DATA[9] = "926.34698853";
            //CH_BASE_DATA[10] = "1025.55870976";
            //CH_BASE_DATA[11] = "1126.34698853";
            //CH_BASE_DATA[12] = "125.55870976";
            //CH_BASE_DATA[13] = "1326.34698853";
            //CH_BASE_DATA[14] = "1425.55870976";
            //CH_BASE_DATA[15] = "1526.34698853";

        }


        /// <summary>
        // step = 0 ==> base 값 수신 완료
        // step = 1 ==> cycle15 값 수신
        // step = 2 ==> cycle20 값 수신
        // step = 3 ==> cycle25 값 수신
        // step = 4 ==> cycle30 값 수신
        // step = 5 ==> cycle35 값 수신
        // step = 6 ==> cycle40 값 수신
        // step = 7 ==> final 값 수신
        /// </summary>
        /// <param name="step"></param>
        /// 

        public bool DoesOpticDataExist(int compareVal)
        {
            for (int i = 0; i < 60; i++)
            {
                if (Plotter.Optic_Measure_Idx[i] == compareVal)
                    return true;
            }
            return false;
        }
        public void MatchAndFindOpticDataForResult()
        {
            string sPath;
            string sFileName;

            sPath = Path.Combine(Environment.CurrentDirectory, "log");
            //sFileName = Path.Combine(sPath, ".txt");

            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(sPath);
            foreach (System.IO.FileInfo File in di.GetFiles())
            {
                if (File.Extension.ToLower().CompareTo(".txt") == 0)
                {
                    String FileNameOnly = File.Name.Substring(0, File.Name.Length - 4);
                    String FullFileName = File.FullName;
                    //MessageBox.Show(FullFileName + " " + FileNameOnly);
                }
            }
            string fileName = "";
            if (checkBox3.Checked)
            {
                fileName = sm.currentLogFileName;
            }
            else
            {
                fileName = sPath + @"\" + sm.current_Log_Name + ".txt"; //di.ToString() + "\\" + str + ".rcp";
            }
            //string fileName = sPath + @"\" + "temp" + ".txt"; //di.ToString() + "\\" + str + ".rcp";

            //string fileName = sPath + @"\Pcr 2021-02-24 17-46-11.txt";//@"\Pcr 2021-02-16 13-25-31" + ".txt";


            int line_count = 0;
            string[] lines = File.ReadAllLines(fileName);
            line_count = File.ReadAllLines(fileName).Length;

            for (int i = 0; i < line_count; i++)
            {
                if (lines[i].Contains("optd") && sm.measured_cnt > -1)
                //if (lines[i].Contains("routine_cnt"))
                {
                    string temp = lines[i];

                    char[] sep = { ',' };
                    string[] result = temp.Split(sep);

                    char[] sep2 = { '=' };
                    string[] routine_cnt = result[1].Split(sep2);

                    Int32.TryParse(routine_cnt[1], out iRoutine_cnt);

                    string[] tube_no = result[2].Split(sep2);
                    Int32.TryParse(tube_no[1], out iTube_no);

                    string[] dye = result[3].Split(sep2);

                    int dye_idx = 0;
                    if (dye[0] == "e1d1[0]")
                    {
                        dye_idx = (int)TUBE_INDEX.FAM;
                    }
                    else if (dye[0] == "e2d2[0]")
                    {
                        dye_idx = (int)TUBE_INDEX.HEX;
                    }
                    else if (dye[0] == "e1d1[1]")
                    {
                        dye_idx = (int)TUBE_INDEX.ROX;
                    }
                    else if (dye[0] == "e2d2[1]")
                    {
                        dye_idx = (int)TUBE_INDEX.CY5;
                    }

                    Int32.TryParse(dye[1], out iDye);

                    for (int j = 0; j < Plotter.COL_CNT; j++)
                    {
                        if (Plotter.Optic_Measure_Idx[j] == iRoutine_cnt)
                        {
                            Plotter.isOpticData_buffer_filled[j]++;
                            MEASURED_DATA[4 * (iTube_no - 1) + dye_idx, j] = iDye.ToString();
                            DISPLAY_DATA[4 * (iTube_no - 1) + dye_idx, j] = iDye.ToString();
                            if (MEASURED_DATA[4 * (iTube_no - 1) + dye_idx, j] == "0")
                            {
                                //MEASURED_DATA[4 * (iTube_no - 1) + dye_idx, j] = "";
                            }

                            if (iRoutine_cnt > sm.Routine_Cnt
                                && Plotter.isOpticData_buffer_filled[sm.measured_cnt] >= Plotter.CH_CNT * Plotter.DYE_CNT
                                && sm.measured_cnt > -1)
                            {
                                Plotter.isOpticData_buffer_filled[sm.measured_cnt] = 0;

                                if (sm.measured_cnt < Plotter.COL_CNT - 1)
                                {
                                    //++sm.measured_cnt;//sm.measured_cnt = -1;
                                }
                                else if (sm.measured_cnt >= Plotter.COL_CNT - 1)
                                {
                                    sm.measured_cnt = -1;
                                }

                                sm.DataUpdateFlag = true;

                                //sm.Routine_Cnt = iRoutine_cnt;
                            }

                        }
                    }
                }
                else if (lines[i].Contains("pel>Cycledone\n") || lines[i].Contains("pel>Cycledone"))
                {
                    //_endProcess();
                    //sm.ProcessEndFlag = true;
                }
            }

        }

        public bool isOpticDataLastBufferFilled()
        {
            int temp;
            for (int i = 0; i < 16; i++)
            {
                Int32.TryParse(MEASURED_DATA[i, Plotter.COL_CNT-1], out temp);
                if (  temp > 0 )
                {
                    //pass
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public void MatchAndFindOpticDataForResultForRealTime()
        {
            string sPath;
            string sFileName;

            sPath = Path.Combine(Environment.CurrentDirectory, "log");
            //sFileName = Path.Combine(sPath, ".txt");

            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(sPath);
            foreach (System.IO.FileInfo File in di.GetFiles())
            {
                if (File.Extension.ToLower().CompareTo(".txt") == 0)
                {
                    String FileNameOnly = File.Name.Substring(0, File.Name.Length - 4);
                    String FullFileName = File.FullName;
                    //MessageBox.Show(FullFileName + " " + FileNameOnly);
                }
            }
            string fileName = "";
            if (checkBox3.Checked)
            {
                fileName = sm.currentLogFileName;
            }
            else
            {
                fileName = sPath + @"\" + sm.current_Log_Name + ".txt"; //di.ToString() + "\\" + str + ".rcp";
            }
            //string fileName = sPath + @"\" + "temp" + ".txt"; //di.ToString() + "\\" + str + ".rcp";

            //string fileName = sPath + @"\Pcr 2021-02-24 17-46-11.txt";//@"\Pcr 2021-02-16 13-25-31" + ".txt";

            try
            {
                int line_count = 0;

                //string[] lines = File.ReadAllLines(fileName);
                allDataArray = allData.Split('\n');
                line_count = allDataArray.Length;//line_count = File.ReadAllLines(fileName).Length;

                for (int i = 0; i < line_count; i++)
                {
                    //if (lines[i].Contains("optd") && sm.measured_cnt > -1)
                    if (allDataArray[i].Contains("optd") && sm.measured_cnt > -1)
                    {
                        string temp = allDataArray[i];

                        char[] sep = { ',' };
                        string[] result = temp.Split(sep);

                        char[] sep2 = { '=' };
                        string[] routine_cnt = result[1].Split(sep2);

                        Int32.TryParse(routine_cnt[1], out iRoutine_cnt);

                        string[] tube_no = result[2].Split(sep2);
                        Int32.TryParse(tube_no[1], out iTube_no);

                        string[] dye = result[3].Split(sep2);

                        int dye_idx = 0;
                        if (dye[0] == "e1d1[0]")
                        {
                            dye_idx = (int)TUBE_INDEX.FAM;
                        }
                        else if (dye[0] == "e2d2[0]")
                        {
                            dye_idx = (int)TUBE_INDEX.HEX;
                        }
                        else if (dye[0] == "e1d1[1]")
                        {
                            dye_idx = (int)TUBE_INDEX.ROX;
                        }
                        else if (dye[0] == "e2d2[1]")
                        {
                            dye_idx = (int)TUBE_INDEX.CY5;
                        }

                        Int32.TryParse(dye[1], out iDye);

                        for (int j = 0; j < Plotter.COL_CNT; j++)
                        {
                            if (Plotter.Optic_Measure_Idx[j] == iRoutine_cnt)
                            {
                                Plotter.isOpticData_buffer_filled[j]++;
                                MEASURED_DATA[4 * (iTube_no - 1) + dye_idx, j] = iDye.ToString();
                                DISPLAY_DATA[4 * (iTube_no - 1) + dye_idx, j] = iDye.ToString();
                                if (MEASURED_DATA[4 * (iTube_no - 1) + dye_idx, j] == "0")
                                {
                                    //MEASURED_DATA[4 * (iTube_no - 1) + dye_idx, j] = "";
                                }
                                //sm.DataUpdateFlag = true;


                                if (iRoutine_cnt > sm.Routine_Cnt
                                    && Plotter.isOpticData_buffer_filled[sm.measured_cnt] >= Plotter.CH_CNT * Plotter.DYE_CNT
                                    && sm.measured_cnt > -1)
                                {
                                    Plotter.isOpticData_buffer_filled[sm.measured_cnt] = 0;

                                    if (sm.measured_cnt < Plotter.COL_CNT - 1)
                                    {
                                        //++sm.measured_cnt;//sm.measured_cnt = -1;
                                    }
                                    else if (sm.measured_cnt >= Plotter.COL_CNT - 1)
                                    {
                                        sm.measured_cnt = -1;
                                    }
                                    //sm.Routine_Cnt = iRoutine_cnt;
                                }

                            }
                        }
                    }
                    else if (allDataArray[i].Contains("pel>Cycledone\n") || allDataArray[i].Contains("pel>Cycledone")) //lines[i].Contains("g_end_process") 
                        //&& processStep == 9
                        //&& sm.measured_cnt == -1
                        //&& !sm.DataUpdateFlag)
                    {
                        //_endProcess();
                        sm.ProcessEndFlag = true;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                //logToFile.Append(ex.ToString());
            }


        }

        public void checkRoutineCntChanged()
        {
            if (sm.pre_routine_cnt == routine_cnt - 1
                || sm.pre_routine_cnt == routine_cnt - 2
                || sm.pre_routine_cnt == routine_cnt - 3
                || sm.pre_routine_cnt == routine_cnt - 4
                || sm.pre_routine_cnt == routine_cnt - 5)//if (sm.pre_routine_cnt == routine_cnt - 1 && Optic_Measure_Index_Flag[sm.pre_routine_cnt] == true)
            {
                Plotter.ResetAllPlots(formsPlot1, formsPlot2, formsPlot3, formsPlot4);
                MatchAndFindOpticDataForResult();
                sm.opticReceivedFlag = true;
                //setGridValue(7);
            }
            else if (routine_cnt == 45)
            {
                Plotter.ResetAllPlots(formsPlot1, formsPlot2, formsPlot3, formsPlot4);
                MatchAndFindOpticDataForResult();
                sm.opticReceivedFlag = true;
                sm.pre_routine_cnt = routine_cnt;
            }
            else
            {
                sm.pre_routine_cnt = routine_cnt;

            }
            /*    
            // databuffer transfer
            if(sm.opticReceivedFlag && sm.pre_routine_cnt == 7 && Optic_Measure_Index_Flag[sm.pre_routine_cnt] == true)
            {
                
                //int rand = 0;
                for (int i = 0; i < 16; i++)
                {
                    //rand = r.Next(500, 2500);
                    //CH_BASE_DATA[i] = rand.ToString();
                    MEASURED_DATA[i, sm.measured_cnt] = CH_BASE_DATA[i];
                }
                sm.pre_routine_cnt = routine_cnt;
                ++sm.measured_cnt;    

            }
            else if(sm.opticReceivedFlag && sm.pre_routine_cnt > 7 && Optic_Measure_Index_Flag[sm.pre_routine_cnt] == true)
            {
                //int rand = 0;
                for (int i = 0; i < 16; i++)
                {
                    //rand = r.Next(500, 2500);
                    //ALL_DATA[i, routine_cnt - 1] = rand.ToString();
                    MEASURED_DATA[i, sm.measured_cnt] = ALL_DATA[i, sm.pre_routine_cnt];
                }
                sm.pre_routine_cnt = routine_cnt;
                ++sm.measured_cnt;
            }
            */
        }

        static int col_Idx;

        private void setGridValue(int step)
        {
            int[] ic_value = new int[16];
            col_Idx = sm.measured_cnt;

            checkRoutineCntChanged();

            //update_IC_result();// test

            if (sm.opticReceivedFlag)
            {
                if (step > 0)  // base 값이 아닌 경우
                {
                    DataGridView[] dgvArr = { dataGridView1_Engineer, dataGridView2_Engineer, dataGridView3_Engineer, dataGridView4_Engineer };
                    DataGridView[] dgvArr2 = { dataGridView1_Tester, dataGridView2_Tester, dataGridView3_Tester, dataGridView4_Tester };
                    DataGridView[] dgvArrCt = { dgv_opticDatum_tube1, dgv_opticDatum_tube2, dgv_opticDatum_tube3, dgv_opticDatum_tube4 };

                    sm.opticReceivedFlag = false;
                    Plotter.isValueBase = false;

                    for (int i = 0; i < 16; i++)
                    {
                        ic_value[i] = 0;
                    }
                    //(int)value_ExceededBase(dataGridView5, 2, 28, 2500); 

                    for (int ch_idx = 0; ch_idx < Plotter.CH_CNT; ch_idx++) //CH_CNT = 4
                    {
                        int idx = ch_idx * 4;
                        // 챔버인덱스(1~4), 컬럼인덱스(1~8), 데이터4개
                        // 각 사이클 데이터 입력시
                        /*
                        string[] result = CriticalThresholdCalculation(getDataGridArray(dgvArrCt[ch_idx], 3, 28));
                        lb_IC_Result.Text = result[0] + "* Value : " + result[1];
                        int tempValue = 0;  Int32.TryParse(result[1], out tempValue);
                        */
                        for (col_Idx = 0; col_Idx < Plotter.COL_CNT; col_Idx++)
                        {

                            //setDataGrid_Ct(dgvArrCt[ch_idx], ch_idx + 1, col_Idx, MEASURED_DATA[idx, col_Idx], MEASURED_DATA[idx + 1, col_Idx], MEASURED_DATA[idx + 2, col_Idx], MEASURED_DATA[idx + 3, col_Idx], ic_value);
                        }

                        //setDataGrid_Ct(dgvArrCt[ch_idx], ch_idx + 1, col_idx + 1, ALL_DATA[idx, col_idx], ALL_DATA[idx + 1, col_idx], ALL_DATA[idx + 2, col_idx], ALL_DATA[idx + 3, col_idx]);
                        // setDataGrid_Engineer(dgvArr[ch_idx], ch_idx + 1, col_idx + 1, ALL_DATA[idx, col_idx], ALL_DATA[idx+1, col_idx], ALL_DATA[idx+2, col_idx], ALL_DATA[idx+3, col_idx]);
                        //setDataGrid_Engineer(dgvArr[ch_idx], ch_idx + 1, col_idx + 1, ALL_DATA[idx, col_idx], ALL_DATA[idx + 1, col_idx], ALL_DATA[idx + 2, col_idx], ALL_DATA[idx + 3, col_idx]);
                    }


                    if (step == 7 && routine_cnt == 45) // final의 경우 검사자 그리드 업데이트
                    {
                        endTestInfo();
                        update_analytic_result();//updateAnalyticResult(selectedDgv);
                        update_IC_result();
                        sm.opticReceivedFlag = false;
                        routine_cnt = 0;
                        //showLog("분석 프로세스가 정상 종료됨");
                        //_endProcess();

                        //UpdateICResult();
                        /*
                        string[] result = CriticalThresholdCalculation(getDataGridArray(dataGridView5, 1, 28));
                        lb_IC_Result.Text = result[0] +    "* Value : " + result[1];

                        int tempValue = 0;
                        Int32.TryParse(result[1], out tempValue);

                        Plotter.DrawHLine(formsPlot1, tempValue);
                        */

                        /*
                        for (int ch_idx = 0; ch_idx < CH_CNT; ch_idx++)
                        {
                            int idx = ch_idx * 4;
                            // 챔버인덱스(1~4), 데이터4개
                            // 각 사이클 데이터 입력시
                            //setDataGrid_Tester(dgvArr2[ch_idx], ch_idx + 1, ALL_DATA[idx, 6], ALL_DATA[idx + 1, 6], ALL_DATA[idx + 2, 6], ALL_DATA[idx + 3, 6]);
                        }
                        */
                    }
                }
                else
                {       // base 값인 경우
                    DataGridView[] dgvArr = { dataGridView1_Engineer, dataGridView2_Engineer, dataGridView3_Engineer, dataGridView4_Engineer };
                    DataGridView[] dgvArrCt = { dgv_opticDatum_tube1, dgv_opticDatum_tube2, dgv_opticDatum_tube3, dgv_opticDatum_tube4 };

                    Plotter.isValueBase = true;
                    sm.opticReceivedFlag = false;

                    //ic_value = 0; // (int)value_ExceededBase(dataGridView5, 2, 28, 2500);

                    for (int ch_idx = 0; ch_idx < Plotter.CH_CNT; ch_idx++)
                    {
                        int idx = ch_idx * 4;
                        // 챔버인덱스(1~4), 컬럼인덱스(1~8), 데이터4개
                        // 각 사이클 데이터 입력시
                        //setDataGrid_Ct(dgvArrCt[ch_idx], ch_idx + 1, 0, MEASURED_DATA[idx, 0], MEASURED_DATA[idx + 1, 0], MEASURED_DATA[idx + 2, 0], MEASURED_DATA[idx + 3, 0], ic_value);
                    }
                    //CH_BASE_DATA
                }
            }
            sm.pre_routine_cnt = routine_cnt;

        }

        /// <summary>
        // step = 0 ==> base 값 수신 완료
        // step = 1 ==> cycle15 값 수신
        // step = 2 ==> cycle20 값 수신
        // step = 3 ==> cycle25 값 수신
        // step = 4 ==> cycle30 값 수신
        // step = 5 ==> cycle35 값 수신
        // step = 6 ==> cycle40 값 수신
        // step = 7 ==> final 값 수신
        /// </summary>
        /// <param name="step"></param>
        private void setProgressInfo(int step)
        {
            if (step == -1)
            {
                progressBar_Tester.Value = 0;  // 상태바만 초기화
                progressBar_Manager.Value = 0;  // 상태바만 초기화

                return;
            }

            setGridValue(step);

            // 각 단계별 처리할 것이 있으면 여기서 처리
            switch (step)
            {
                case 0:  // base 값 업데이트
                    break;
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;
                case 6:
                    break;
                case 7:
                    break;
            }
        }
        private void progressBar_Tester_Click(object sender, EventArgs e)
        {

        }
        private void btn_Regist_IDManage_Click(object sender, EventArgs e)
        {
            // 등록할 내용이 없으면 리턴
            if (tb_Name_IDManage.Text == "" || tb_ID_IDManage.Text == "" || tb_PW_IDManage.Text == "")
            {
                MessageBox.Show("성명, ID, Password 모두 입력하세요.", "계정 등록 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //MessageBox.Show("Name, ID, Password must be .", "계정 등록 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 신규 계정 등록
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists) {
                di.Create();
            }
            string fileName = di.ToString() + "\\member.info";

            StreamWriter sw = new StreamWriter(fileName, true);

            string buff = tb_Name_IDManage.Text + "," + tb_ID_IDManage.Text + "," + tb_PW_IDManage.Text + "," + tb_Right_IDManage.SelectedItem;

            sw.WriteLine(buff);

            sw.Close();

            MessageBox.Show("계정이 등록되었습니다.", "계정 등록 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btn_Search_IDManage_Click(this, null);

            tb_Name_IDManage.Text = "";
            tb_ID_IDManage.Text = "";
            tb_PW_IDManage.Text = "";
        }
        private void btn_Remove_IDManage_Click(object sender, EventArgs e)
        {
            if (tb_Right_IDManage.Text == "engineer") // 관리자 계정 삭제 불가
            {
                MessageBox.Show("엔지니어 계정은 삭제할 수 없습니다.", "계정 삭제 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            else if (tb_delPW_IDManage.Text == "")
            {
                MessageBox.Show("삭제할 계정을 선택 후 암호를 입력해야 삭제할 수 있습니다.", "계정 삭제 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (tb_delPW_IDManage.Text != tb_delPW2_IDManage.Text) {
                MessageBox.Show("암호가 일치해야 삭제할 수 있습니다.", "계정 삭제 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");

                string fileName = di.ToString() + "\\member.info";
                Save_Csv(fileName, dataGridView_Manage, true);

                tb_delName_IDManage.Text = "";
                tb_delID_IDManage.Text = "";
                tb_delPW_IDManage.Text = "";
                tb_delPW2_IDManage.Text = "";
            }
        }
        private void btn_Search_IDManage_Click(object sender, EventArgs e)
        {
            // 계정 조회
            dataGridView_Manage.Rows.Clear();

            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists) di.Create();

            string fileName = di.ToString() + "\\member.info";

            string[] lines = File.ReadAllLines(fileName);

            int readNum = 1;
            string temp = "";
            for (int i = 1; i < lines.Length; i++) //데이터가 존재하는 라인일 때에만, label에 출력한다.
            {
                temp = lines[i];

                char[] sep = { ',' };

                string[] result = temp.Split(sep);
                //string[] data6 = new string[4] { temp, temp, temp, temp };
                int index = 0;
                //foreach (var item in result)
                //{
                //    data6[index++] = item;
                //}

                dataGridView_Manage.Rows.Add(result);
            }

            for (int i = 0; i < dataGridView_Manage.Rows.Count; i++)
            {
                if (i % 2 != 0)
                {
                    dataGridView_Manage.Rows[i].DefaultCellStyle.BackColor = Color.White;
                }
                else
                {
                    dataGridView_Manage.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                }
            }
        }
        private void dataGridView_Manage_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // 선택한 셀의 정보를 채움
            if (e.RowIndex == 0 && dataGridView_Manage.RowCount == 1 || dataGridView_Manage.Rows.Count == (e.RowIndex + 1))
                return;
            tb_delName_IDManage.Text = dataGridView_Manage.Rows[e.RowIndex].Cells[0].Value.ToString();
            tb_delID_IDManage.Text = dataGridView_Manage.Rows[e.RowIndex].Cells[1].Value.ToString();
            tb_delPW2_IDManage.Text = dataGridView_Manage.Rows[e.RowIndex].Cells[2].Value.ToString();

            selectedRowCount = e.RowIndex;

        }

        private void Save_Csv_BaseCycles(string fileName, DataGridView dgv, bool header)
        {
            // 현재 선택한 셀의 정보를 삭제 후
            //dataGridView_Manage.Rows.RemoveAt(this.dataGridView_Manage.SelectedRows[0].Index);
            //int sIndex = dgv.CurrentCell.RowIndex;
            //dgv.Rows.RemoveAt(sIndex);

            // 그리드뷰를 파일로 저장함
            string delimiter = ",";  // 구분자
            FileStream fs = new FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            StreamWriter csvExport = new StreamWriter(fs, System.Text.Encoding.UTF8);

            if (dgv.Rows.Count == 0) return;

            // 헤더정보 출력
            if (header)
            {
                for (int i = 0; i < dgv.Columns.Count; i++)
                {
                    csvExport.Write(dgv.Columns[i].HeaderText);
                    if (i != dgv.Columns.Count - 1)
                    {
                        csvExport.Write(delimiter);
                    }
                }
            }

            csvExport.Write(csvExport.NewLine); // add new line

            // 데이터 출력
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (!row.IsNewRow)
                {
                    for (int i = 0; i < dgv.Columns.Count; i++)
                    {
                        csvExport.Write(row.Cells[i].Value);
                        if (i != dgv.Columns.Count - 1)
                        {
                            csvExport.Write(delimiter);
                        }
                    }
                    csvExport.Write(csvExport.NewLine); // write new line
                }
            }

            csvExport.Flush(); // flush from the buffers.
            csvExport.Close();
            fs.Close();
            MessageBox.Show("Base cycle Information Saved.", "Info Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 계정을 1개 삭제 후 dataGridView 데이터를 파일 내보내기
        /// </summary>
        private void Save_Csv(string fileName, DataGridView dgv, bool header)
        {
            // 현재 선택한 셀의 정보를 삭제 후
            //dataGridView_Manage.Rows.RemoveAt(this.dataGridView_Manage.SelectedRows[0].Index);
            int sIndex = dataGridView_Manage.CurrentCell.RowIndex;

            dataGridView_Manage.Rows.RemoveAt(sIndex);

            // 그리드뷰를 파일로 저장함
            string delimiter = ",";  // 구분자
            FileStream fs = new FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            StreamWriter csvExport = new StreamWriter(fs, System.Text.Encoding.UTF8);

            if (dgv.Rows.Count == 0) return;

            // 헤더정보 출력
            if (header)
            {
                for (int i = 0; i < dgv.Columns.Count; i++)
                {
                    csvExport.Write(dgv.Columns[i].HeaderText);
                    if (i != dgv.Columns.Count - 1)
                    {
                        csvExport.Write(delimiter);
                    }
                }
            }

            csvExport.Write(csvExport.NewLine); // add new line

            // 데이터 출력
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (!row.IsNewRow)
                {
                    for (int i = 0; i < dgv.Columns.Count; i++)
                    {
                        csvExport.Write(row.Cells[i].Value);
                        if (i != dgv.Columns.Count - 1)
                        {
                            csvExport.Write(delimiter);
                        }
                    }
                    csvExport.Write(csvExport.NewLine); // write new line
                }
            }

            csvExport.Flush(); // flush from the buffers.
            csvExport.Close();
            fs.Close();
            MessageBox.Show("계정을 삭제하였습니다.", "계정 삭제 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Save_GridCsv(string fileName)
        {
            //DataGridView[] dgvArr = { dataGridView1_Engineer, dataGridView2_Engineer, dataGridView3_Engineer, dataGridView4_Engineer };
            DataGridView[] dgvArr = { dgv_opticDatum_tube1, dgv_opticDatum_tube2, dgv_opticDatum_tube3, dgv_opticDatum_tube4 };

            // 그리드뷰를 파일로 저장함
            string delimiter = ",";  // 구분자
            FileStream fs = new FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            StreamWriter csvExport = new StreamWriter(fs, System.Text.Encoding.UTF8);

            if (dgvArr[0].Rows.Count == 0) return;

            // 헤더정보 출력
            if (true)
            {
                for (int i = 0; i < dgvArr[0].Columns.Count; i++)
                {
                    csvExport.Write(dgvArr[0].Columns[i].HeaderText);
                    if (i != dgvArr[0].Columns.Count - 1)
                    {
                        csvExport.Write(delimiter);
                    }
                }
            }

            csvExport.Write(csvExport.NewLine); // add new line

            // 데이터 출력
            for (int n = 0; n < 4; n++)
            {
                foreach (DataGridViewRow row in dgvArr[n].Rows)
                {
                    if (!row.IsNewRow)
                    {
                        for (int i = 0; i < dgvArr[n].Columns.Count; i++)
                        {
                            csvExport.Write(row.Cells[i].Value);
                            if (i != dgvArr[n].Columns.Count - 1)
                            {
                                csvExport.Write(delimiter);
                            }
                        }
                        csvExport.Write(csvExport.NewLine); // write new line
                    }
                }
            }
            csvExport.Flush(); // flush from the buffers.
            csvExport.Close();
            fs.Close();
        }



        private void btn_NewRecipe_Eng_Click(object sender, EventArgs e)
        {
            // 새로운 레시피를 만들고 싶으면 레시피 콤보박스에 이름을 넣은 후 클릭
            // 레시피 이름 입력 체크
            int index = cb_Recipe_Eng.SelectedIndex;    // -1 이면 입력한 것이 없음
            string str = cb_Recipe_Eng.Text;

            if (index == -1 && str != "")
            {
                MessageBox.Show("새로운 레시피 파일을 생성했습니다. 설정값을 입력 후 저장해야 합니다.", "레시피 생성 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 새로운 레시피 파일 저장
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
                if (!di.Exists)
                {
                    di.Create();
                }
                string fileName = di.ToString() + "\\" + str + ".rcp";

                // 기존 파일 삭제
                FileInfo fileDel = new FileInfo(fileName);
                if (fileDel.Exists) fileDel.Delete(); // 없어도 에러안남

                // 새로 저장
                StreamWriter sw = new StreamWriter(fileName, true);

                string buff = tb_RT_PreTemp_Eng.Text + "," + tb_RT_PreHoldSec_Eng.Text + "," + tb_PreTemp_Eng.Text + "," + tb_PreHoldSec_Eng.Text + "," + tb_1Temp_Eng.Text + "," + tb_1HoldSec_Eng.Text
                     + "," + tb_2Temp_Eng.Text + "," + tb_2HoldSec_Eng.Text + "," + tb_OCDelaySec_Eng.Text + "," + tb_OCHoldSec_Eng.Text
                     + "," + tb_FianlCycle_Eng.Text;

                sw.WriteLine(buff);

                sw.Close();
                // 콤보 박스 업데이트
                SetRecipeCombobox_Test();
                cb_Recipe_Eng.SelectedIndex = cb_Recipe_Eng.Items.Count - 1;
                //create Analytic result DataGridView


                bLoadedRecipe = false;

               
            }
            else if (index == -1 || str == "")
            {
                MessageBox.Show("레시피 이름을 입력한 후 생성해 주세요.", "레시피 생성 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("기존에 존재하는 레시피 이름인지 확인해 주세요.", "레시피 생성 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
      

        private void btn_DelRecipe_Eng_Click(object sender, EventArgs e)
        {
            // 선택한 레시피를 불러옴
            int index = cb_Recipe_Eng.SelectedIndex;    // -1 이면 입력한 것이 없음
            if (index == -1)
            {
                MessageBox.Show("레시피를 선택해 주세요.", "레시피 삭제 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string str = cb_Recipe_Eng.Text;
            // 현재 레시피 파일을 찾은 후 삭제
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists)
            {
                di.Create();
            }
            string fileName = di.ToString() + "\\" + str + ".rcp";

            FileInfo fileDel = new FileInfo(fileName);
            if (fileDel.Exists) // 삭제할 파일이 있는지
            {
                if (DialogResult.OK == MessageBox.Show("레시피를 삭제하겠습니까?", "레시피 삭제 안내", MessageBoxButtons.OKCancel, MessageBoxIcon.Information)) {
                    fileDel.Delete(); // 없어도 에러안남

                    // 콤보 박스 업데이트
                    SetRecipeCombobox_Test();
                    cb_Recipe_Eng.SelectedIndex = 0;

                    // 보여주는 값을 초기화
                    tb_RT_PreTemp_Eng.Text = "-";
                    tb_RT_PreHoldSec_Eng.Text = "-";
                    tb_PreTemp_Eng.Text = "-";
                    tb_PreHoldSec_Eng.Text = "-";
                    tb_1Temp_Eng.Text = "-";
                    tb_1HoldSec_Eng.Text = "-";
                    tb_2Temp_Eng.Text = "-";
                    tb_2HoldSec_Eng.Text = "-";
                    tb_OCDelaySec_Eng.Text = "-";
                    tb_OCHoldSec_Eng.Text = "-";
                    tb_FianlCycle_Eng.Text = "-";
                    tb_OpticCurrentFAM.Text = "-";
                    tb_OpticCurrentROX.Text = "-";
                    tb_OpticCurrentHEX.Text = "-";
                    tb_OpticCurrentCY5.Text = "-";
                    tb_MachineNum_Eng.Text = "-";

                    MessageBox.Show("레시피를 삭제하였습니다.", "레시피 삭제 안내", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);

                    bLoadedRecipe = false;
                }
            }
        }

        private void btn_SaveRecipe_Eng_Click(object sender, EventArgs e)
        {
            // 선택한 레시피를 불러옴
            int index = cb_Recipe_Eng.SelectedIndex;    // -1 이면 입력한 것이 없음
            if (index == -1)
            {
                MessageBox.Show("레시피를 선택해 주세요.", "레시피 저장 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string str = cb_Recipe_Eng.Text;

            // 새로운 레시피 파일 저장
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists) di.Create();

            string fileName = di.ToString() + "\\" + str + ".rcp";

            // 기존 파일 삭제
            FileInfo fileDel = new FileInfo(fileName);
            if (fileDel.Exists) fileDel.Delete(); // 없어도 에러안남

            // 새로 저장
            StreamWriter sw = new StreamWriter(fileName, true);

            string buff = tb_MachineNum_Eng.Text + "," + tb_RT_PreTemp_Eng.Text + "," +tb_RT_PreHoldSec_Eng.Text + ","  + tb_PreTemp_Eng.Text + "," + tb_PreHoldSec_Eng.Text + "," + tb_1Temp_Eng.Text + "," + tb_1HoldSec_Eng.Text
                    + "," + tb_2Temp_Eng.Text + "," + tb_2HoldSec_Eng.Text + "," + tb_OCDelaySec_Eng.Text + "," + tb_OCHoldSec_Eng.Text
                    + "," + tb_FianlCycle_Eng.Text + "," + tb_OpticCurrentFAM.Text + "," + tb_OpticCurrentROX.Text + "," + tb_OpticCurrentHEX.Text + "," + tb_OpticCurrentCY5.Text;

            sw.WriteLine(buff);
            sw.Close();
            // 콤보 박스 업데이트
            SetRecipeCombobox_Test();

            MessageBox.Show("레시피를 저장하였습니다.", "레시피 저장 안내", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
        }

        private void btn_LoadRecipe_Eng_Click(object sender, EventArgs e)
        {
            // 선택한 레시피를 불러옴
            int index = cb_Recipe_Eng.SelectedIndex;    // -1 이면 입력한 것이 없음
            if (index == -1)
            {
                MessageBox.Show("레시피를 선택해 주세요.", "레시피 읽기 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            cb_Recipe_Test.SelectedIndex = cb_Recipe_Eng.SelectedIndex;    // -1 이면 입력한 것이 없음

            bLoadedRecipe = true;

            string str = cb_Recipe_Eng.Text;

            // 새로운 레시피 파일 저장
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists) di.Create();

            string fileName = di.ToString() + "\\" + str + ".rcp";

            string[] lines = File.ReadAllLines(fileName);

            string temp = "";
            temp = lines[0]; // 1줄 밖에 없음

            char[] sep = { ',' };

            string[] result = temp.Split(sep);

            tb_MachineNum_Eng.Text = result[0];
            sm.machineNumber = result[0];
            tb_RT_PreTemp_Eng.Text = result[1]; 
            tb_RT_PreHoldSec_Eng.Text = result[2];
            tb_PreTemp_Eng.Text = result[3];
            tb_PreHoldSec_Eng.Text = result[4];
            tb_1Temp_Eng.Text = result[5];
            tb_1HoldSec_Eng.Text = result[6];
            tb_2Temp_Eng.Text = result[7];
            tb_2HoldSec_Eng.Text = result[8];
            tb_OCDelaySec_Eng.Text = result[9];
            tb_OCHoldSec_Eng.Text = result[10];
            tb_FianlCycle_Eng.Text = result[11];
            tb_OpticCurrentFAM.Text = result[12];
            tb_OpticCurrentROX.Text = result[13];
            tb_OpticCurrentHEX.Text = result[14];
            tb_OpticCurrentCY5.Text = result[15];
        }

        private void btn_MCURead_Eng_Click(object sender, EventArgs e)
        {
            // MCU와 통신하여 설정된 값을 가져와서 보여줌
            /*
            string[] result = { "1", "2", "3", "4", "5", "6", "7", "8", "9"};

            tb_PreTempMCU_Eng.Text = result[0];
            tb_PreHoldSecMCU_Eng.Text = result[1];
            tb_1TempMCU_Eng.Text = result[2];
            tb_1HoldSecMCU_Eng.Text = result[3];
            tb_2TempMCU_Eng.Text = result[4];
            tb_2HoldSecMCU_Eng.Text = result[5];
            tb_OCDelaySecMCU_Eng.Text = result[6];
            tb_OCHoldSecMCU_Eng.Text = result[7];
            tb_FianlCycleMCU_Eng.Text = result[8];
            */
        }

        private void btn_ApplyAll_Eng_Click(object sender, EventArgs e)
        {

        }

        private void tb_PW_Main_KeyUp(object sender, KeyEventArgs e)
        {
            // 엔터키 누를 때 로그인 버튼 클릭
            if (e.KeyCode == Keys.Enter && tb_PW_Main.Text != "") {
                btn_Login_Main_Click(this, null);
            }
        }


        private void groupBox6_Enter(object sender, EventArgs e)
        {

        }

        private void tabPage6_Click(object sender, EventArgs e)
        {

        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void label27_Click(object sender, EventArgs e)
        {

        }

        private void label26_Click(object sender, EventArgs e)
        {

        }

        void Read_exec(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(":SHOW_GLOB_VARS");

            String txt = sb.ToString();
            //Apply_txt = txt;
            serial.SendLine(txt);
        }

        void Cycle_exec(object sender, EventArgs e)
        {
            if (tb_FianlCycle_Eng != null && tb_FianlCycle_Eng.Text != "" && tb_FianlCycle_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":FINAL_CYCLE");
                sb.Append(" ");
                sb.Append(tb_FianlCycle_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        void Operation_exec(object sender, EventArgs e)
        {
            if (tb_OCHoldSec_Eng.Text != "" && tb_OCHoldSec_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":OPTIC_OPERATION_KEEPING_TEMP_SEC");
                sb.Append(" ");
                sb.Append(tb_OCHoldSec_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        void NoOperation_exec(object sender, EventArgs e)
        {
            if (tb_2HoldSec_Eng.Text != "" && tb_2HoldSec_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":2nd_STEP_KEEPING_TIME_SEC");
                sb.Append(" ");
                sb.Append(tb_2HoldSec_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        void Before_exec(object sender, EventArgs e)
        {
            if (tb_OCDelaySec_Eng.Text != "" && tb_OCDelaySec_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":DELAY_TIME_BEFORE_OPTING_RUNING");
                sb.Append(" ");
                sb.Append(tb_OCDelaySec_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        void Peltier_exec(object sender, EventArgs e)
        {
            if (tb_PreHoldSec_Eng.Text != "" && tb_PreHoldSec_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":PRECOND_KEEPING_TIME_MIN");
                sb.Append(" ");
                sb.Append(tb_PreHoldSec_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        void High_exec(object sender, EventArgs e)
        {
            if (tb_1HoldSec_Eng.Text != "" && tb_1HoldSec_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":1st_STEP_KEEPING_TIME_SEC");
                sb.Append(" ");
                sb.Append(tb_1HoldSec_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        void Heat_exec(object sender, EventArgs e)
        {
            if (tb_1Temp_Eng.Text != "" && tb_1Temp_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":1st_STEP_SETPOINT");
                sb.Append(" ");
                sb.Append(tb_1Temp_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        void Cool_exec(object sender, EventArgs e)
        {
            if (tb_2Temp_Eng.Text != "" && tb_2Temp_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":2nd_STEP_SETPOINT");
                sb.Append(" ");
                sb.Append(tb_2Temp_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        void Cond_exec(object sender, EventArgs e)
        {
            if (tb_PreTemp_Eng.Text != "" && tb_PreTemp_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":PRE_COND_SETPOINT");
                sb.Append(" ");
                sb.Append(tb_PreTemp_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        void Apply_exec(object sender, EventArgs e)
        {
            if (cb_Recipe_Eng.SelectedIndex == -1 || bLoadedRecipe == false)
            {
                MessageBox.Show("레시피를 선택 한 후 [Load]를 해야 [전체 전송]이 가능합니다.", "설정 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (tb_OCHoldSec_Eng.Text == "" || tb_OCHoldSec_Eng.Text == "-"
            || tb_FianlCycle_Eng.Text == "" || tb_FianlCycle_Eng.Text == "-"
            || tb_2HoldSec_Eng.Text == "" || tb_2HoldSec_Eng.Text == "-"
            || tb_OCDelaySec_Eng.Text == "" || tb_OCDelaySec_Eng.Text == "-"
            || tb_PreHoldSec_Eng.Text == "" || tb_PreHoldSec_Eng.Text == "-"
            || tb_1HoldSec_Eng.Text == "" || tb_1HoldSec_Eng.Text == "-"
            || tb_1Temp_Eng.Text == "" || tb_1Temp_Eng.Text == "-"
            || tb_2Temp_Eng.Text == "" || tb_2Temp_Eng.Text == "-"
            || tb_PreTemp_Eng.Text == "" || tb_PreTemp_Eng.Text == "-"
            || tb_RT_PreTemp_Eng.Text =="" || tb_RT_PreTemp_Eng.Text == "-"
            || tb_RT_PreHoldSec_Eng.Text =="" || tb_RT_PreHoldSec_Eng.Text == "-"
            || tb_OpticCurrentFAM.Text =="" || tb_OpticCurrentFAM.Text == "-"
            || tb_OpticCurrentROX.Text =="" || tb_OpticCurrentROX.Text == "-"
            || tb_OpticCurrentHEX.Text=="" || tb_OpticCurrentHEX.Text =="-"
            || tb_OpticCurrentCY5.Text=="" || tb_OpticCurrentCY5.Text == "-"
            || tb_MachineNum_Eng.Text=="" || tb_MachineNum_Eng.Text=="-")
            {
                MessageBox.Show("설정한 값 중에 전송이 불가능한 값이 있습니다. 모두 숫자여야 합니다.", "설정 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }


            cmd_dic[":FINAL_CYCLE"] = tb_FianlCycle_Eng.Text;
            cmd_dic[":OPTIC_OPERATION_KEEPING_TEMP_SEC"] = tb_OCHoldSec_Eng.Text;
            cmd_dic[":2nd_STEP_KEEPING_TIME_SEC"] = tb_2HoldSec_Eng.Text;
            cmd_dic[":DELAY_TIME_BEFORE_OPTING_RUNING"] = tb_OCDelaySec_Eng.Text;

            
            cmd_dic[":1st_STEP_KEEPING_TIME_SEC"] = tb_1HoldSec_Eng.Text;
            cmd_dic[":1st_STEP_SETPOINT"] = tb_1Temp_Eng.Text;
            cmd_dic[":2nd_STEP_SETPOINT"] = tb_2Temp_Eng.Text;

            cmd_dic[":PRE_COND_SETPOINT"] = tb_PreTemp_Eng.Text;
            cmd_dic[":PRECOND_KEEPING_TIME_MIN"] = tb_PreHoldSec_Eng.Text;

            cmd_dic[":RT_PRE_COND_SETPOINT"] = tb_RT_PreTemp_Eng.Text;
            cmd_dic[":RT_PRECOND_KEEPING_TIME_MIN"] = tb_RT_PreHoldSec_Eng.Text;

            cmd_dic[":OPTIC_CURRENT_FAM"] = tb_OpticCurrentFAM.Text;
            cmd_dic[":OPTIC_CURRENT_ROX"] = tb_OpticCurrentROX.Text;
            cmd_dic[":OPTIC_CURRENT_HEX"] = tb_OpticCurrentHEX.Text;
            cmd_dic[":OPTIC_CURRENT_CY5"] = tb_OpticCurrentCY5.Text;

            cmd_dic[":MACHINE_NUM"] = tb_MachineNum_Eng.Text;
            


            foreach (String cmd in cmd_dic.Keys)
            {
                if (cmd_dic[cmd] != null && cmd_dic[cmd] != "")
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(cmd);
                    sb.Append(" ");
                    sb.Append(cmd_dic[cmd]);

                    String txt = sb.ToString();
                    //Apply_txt = txt;

                    // 연속 전송시 딜레이를 둠
                    Thread.Sleep(200);
                    serial.SendLine(txt);
                }
            }

            MessageBox.Show("레시피 설정 값을 MCU로 전송했습니다.", "설정 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Read_exec(sender, e);
        }

        void SetSMStroke_exec(object sender, EventArgs e)
        {
            String SelectedItemSM = cb_Motor.SelectedItem.ToString();
            String SelectedItemDirSM = cb_Motor_Dir.SelectedItem.ToString();
            int PulseCountSM = Convert.ToInt32(tb_Pulse_Cnt.Text);
            int PulsePeriodSM = Convert.ToInt32(tb_Pulse_Period.Text);

            Console.WriteLine("SM[{0}] dir:{1}, Count:{2} Peroid:{3}", SelectedItemSM, SelectedItemDirSM, PulseCountSM, PulsePeriodSM);

            if ((PulseCountSM > 9999) || (PulseCountSM < 0))
            {
                MessageBox.Show("범위 초과(0~9999)");
                return;
            }

            if ((PulsePeriodSM > 9999) || (PulsePeriodSM < 0))
            {
                MessageBox.Show("범위 초과(0~9999)");
                return;
            }

            StringBuilder sb = new StringBuilder();

            sb.Append(":SM");
            sb.Append(SelectedItemSM);

            int dir = 0;
            if (SelectedItemDirSM == "UP (BACKWARD)")
            {
                dir = 1;
            }
            else if (SelectedItemDirSM == "DOWN (FORWARD)")
            {
                dir = 0;
            }
            sb.Append(dir.ToString());
            sb.Append(string.Format("{0:0000}{1:0000}", PulseCountSM, PulsePeriodSM));

            string txt = sb.ToString();
            Console.WriteLine(txt);
            serial.SendLine(txt);
        }

        void SetOMStroke_exec(object sender, EventArgs e)
        {
            String SelectedItemDirOM = cb_OMotor.Text;
            int PulseCountOM = Convert.ToInt32(tb_OPulse_Cnt.Text);
            int PulsePeriodOM = Convert.ToInt32(tb_OPulse_Period.Text);

            Console.WriteLine("OM dir:{0}, Count:{1} Peroid:{2}", SelectedItemDirOM, PulseCountOM, PulsePeriodOM);

            if ((PulseCountOM > 9999) || (PulseCountOM < 0))
            {
                MessageBox.Show("범위 초과(0~9999)");
                return;
            }

            if ((PulsePeriodOM > 9999) || (PulsePeriodOM < 0))
            {
                MessageBox.Show("범위 초과(0~9999)");
                return;
            }

            StringBuilder sb = new StringBuilder();

            sb.Append(":OM1");
            int dir = 1;
            if (SelectedItemDirOM == "CLOCK")
            {
                dir = 0;
            }
            sb.Append(dir.ToString());
            sb.Append(string.Format("{0:0000}{1:0000}", PulseCountOM, PulsePeriodOM));

            string txt = sb.ToString();
            Console.WriteLine(txt);
            serial.SendLine(txt);
        }

        void DoorLock_exec(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(":DOOR_UNLOCK");

            String txt = sb.ToString();
            serial.SendLine(txt);
        }

        void DoorUnLock_exec(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(":DOOR_LOCK");

            String txt = sb.ToString();
            serial.SendLine(txt);
        }

        void Fan1On_exec(object sender, EventArgs e)
        {
            serial.SendLine(":forceFan");
        }

        void Fan1Off_exec(object sender, EventArgs e)
        {
            serial.SendLine(":FanOff");
        }

        void Fan2On_exec(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(":FAN2_ON");

            String txt = sb.ToString();
            serial.SendLine(txt);
        }

        void Fan2Ooff_exec(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(":FAN2_OFF");

            String txt = sb.ToString();
            serial.SendLine(txt);
        }

        void Optic1_exec(object sender, EventArgs e)
        {
            serial.SendLine(":optic1");
        }

        void Optic2_exec(object sender, EventArgs e)
        {
            serial.SendLine(":optic2");
        }

        void ReadTemp_exec(object sender, EventArgs e)
        {
            serial.SendLine(":readTemp");
        }

        void Feltier_exec(object sender, EventArgs e)
        {
            serial.SendLine(":FELTIER_TEST");
        }

        void GetSerialStringBarcode(object sender)
        {
            byte[] raw;
            strData = serialBarcode.ReceiveString(out raw);

            if (strData.Contains("\r\n"))
            {
                if (id_selectedIndex == (int)ID_INDEX.PATIENT_ID)
                {
                    setText_Control_Invoke(tb_PatientID_Test, strData);//txtBarcodeRead
                }
                else if (id_selectedIndex == (int)ID_INDEX.SAMPLE_ID)
                {
                    setText_Control_Invoke(tb_SampleID_Test, strData);//txtBarcodeRead
                }
                else if (id_selectedIndex == (int)ID_INDEX.CARTRIDGE_ID)
                {
                    setText_Control_Invoke(tb_CartridgeID_Test, strData);//txtBarcodeRead
                }

            }
        }

        void GetSerialString(object sender)
        {
           
                byte[] raw;
                strData = serial.ReceiveString(out raw);
                SetDataBox(strData);
                strData = strData.Replace(" ", "");
                                
                if (strData.Contains("<"))
                {
                    globRecv = true;
                }

                waitData += strData;
                allData += strData;
                
                // allDataArray[sm.logLineCnt] = strData;
                // sm.logLineCnt++;

                if (strData.Contains("-"))
                {
                    globRecv = false;
                }


                if (!globRecv)
                {
                    if (waitData.IndexOf('\n') > -1)
                    {
                        procData = waitData.Split('\n');
                        foreach (string tmp in procData)
                        {
                            SetCommRXProc(tmp);
                            waitData = "";
                        }
                    }
                }

            
                if ((!string.IsNullOrEmpty(strData)) && (strData.Length > 0))
                {
                    logToFile.Append(strData);
                }
               
            
        }

        void GetSerialString4(object sender)
        {
            byte[] raw;
            strData = serial.ReceiveString(out raw);
            SetDataBox(strData);

            strData = strData.Replace(" ", "");
            procData = strData.Split('\n');

            foreach (string tmp in procData)
            {
                string[] arr = tmp.Split('=');
                if (arr.Length > 1)
                {
                    if (arr[1] != null && arr[1] != "")
                    {
                        SetCommRXProc(tmp);
                        waitData = "";
                    }
                    else
                    {
                        waitData += tmp;
                    }
                }
                else
                {
                    waitData += tmp;
                }
            }

            SetCommRXProc(waitData);
        }

        void SetDataBox(string str)
        {
            if (this.InvokeRequired)
            {
                SetDataBoxCallback sdb = new SetDataBoxCallback(SetDataBox);
                this.Invoke(sdb, new object[] { str });
            }
            else
            {
                if (!firstRecv)
                {
                    tb_Data.Text = "";
                    tb_Log.Text = "";

                    firstRecv = true;
                }

                tb_Data.AppendText(str + "\r");
                tb_Data.Select(tb_Data.Text.Length, 0);
                tb_Data.ScrollToCaret();
                tb_Log.AppendText(str + "\r");
                tb_Log.Select(tb_Log.Text.Length, 0);
                tb_Log.ScrollToCaret();
            }
        }

        public void showLog(string str)
        {
            if (this.InvokeRequired)
            {
                SetDataBoxCallback sdb = new SetDataBoxCallback(SetDataBox);
                this.Invoke(sdb, new object[] { str });
            }
            else
            {
                tb_Data.Text += Environment.NewLine;
                tb_Data.AppendText(str);
                //                tb_Data.Text += Environment.NewLine;
                tb_Data.Select(tb_Data.Text.Length, 0);
                tb_Data.ScrollToCaret();
                tb_Log.Text += Environment.NewLine;
                tb_Log.AppendText(str);
                //              tb_Log.Text += Environment.NewLine;
                tb_Log.Select(tb_Log.Text.Length, 0);
                tb_Log.ScrollToCaret();
            }
        }

        public void SetCommRxProc2(string str)
        {
            str = str.Replace(" ", "");
            
        }

        public void SetCommRXProc(string str)
        {
            str = str.Replace(" ", "");
            //str = str.Replace("-", "");
            //str = str.Replace(">", "");
            //str = str.Replace("<", "");

            string[] commVar = str.Split('=');
            if (commVar.Length > 1)
            {
                if (commVar[0].Equals("g_FINAL_CYCLE"))
                {
                    //tb_FianlCycleMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_FianlCycleMCU_Eng, commVar[1]);
                }
                else if (commVar[0].Equals("g_OPTIC_OPERATION_KEEPING_TEMP_SEC"))
                {
                    //tb_OCHoldSecMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_OCHoldSecMCU_Eng, commVar[1]);
                }
                else if (commVar[0].Equals("g_2nd_STEP_KEEPING_TIME_SEC"))
                {
                    //tb_2HoldSecMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_2HoldSecMCU_Eng, commVar[1]);
                }
                else if (commVar[0].Equals("g_DELAY_TIME_BEFORE_OPTING_RUNING"))
                {
                    //tb_OCDelaySecMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_OCDelaySecMCU_Eng, commVar[1]);
                }
                else if (commVar[0].Equals("g_PRECOND_KEEPING_TIME_MIN"))
                {
                    //tb_PreHoldSecMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_PreHoldSecMCU_Eng, commVar[1]);
                }
                else if (commVar[0].Equals("g_1st_STEP_KEEPING_TIME_SEC"))
                {
                    //tb_1HoldSecMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_1HoldSecMCU_Eng, commVar[1]);
                }
                else if (commVar[0].Equals("g_1st_STEP_SETPOINT"))
                {
                    //tb_1TempMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_1TempMCU_Eng, commVar[1]);
                }
                else if (commVar[0].Equals("g_2nd_STEP_SETPOINT"))
                {
                    //tb_2TempMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_2TempMCU_Eng, commVar[1]);
                }
                else if (commVar[0].Equals("g_PRE_COND_SETPOINT"))
                {
                    //tb_PreTempMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_PreTempMCU_Eng, commVar[1]);
                }
                else if (commVar[0].Equals("g_RT_PRE_COND_SETPOINT"))
                {
                    //tb_PreTempMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_RT_PreTempMCU_Eng, commVar[1]);
                }
                else if (commVar[0].Equals("g_RT_PRECOND_KEEPING_TIME_MIN"))
                {
                    //tb_PreTempMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_RT_PreHoldSecMCU_Eng, commVar[1]);
                }
                else if (commVar[0].Equals("g_optic_current_FAM"))
                {
                    //tb_PreTempMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_OpticCurrentFAM_MCU, commVar[1]);
                }
                else if (commVar[0].Equals("g_optic_current_ROX"))
                {
                    //tb_PreTempMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_OpticCurrentROX_MCU, commVar[1]);
                }
                else if (commVar[0].Equals("g_optic_current_HEX"))
                {
                    //tb_PreTempMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_OpticCurrentHEX_MCU, commVar[1]);
                }
                else if (commVar[0].Equals("g_optic_current_CY5"))
                {
                    //tb_PreTempMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_OpticCurrentCY5_MCU, commVar[1]);
                }
              
                else if (commVar[0].Equals("g_rountine_cnt"))
                {
                    //tb_PreTempMCU_Eng.Text = commVar[1];
                    //SetTextBox(tb_PreTempMCU_Eng, commVar[1]);
                    Int32.TryParse(commVar[1], out routine_cnt);
                    sm.Routine_Cnt = routine_cnt;

                    for(int i = 0;  i < Plotter.COL_CNT; i++)
                    {
                        if(sm.Routine_Cnt == Plotter.Optic_Measure_Idx[i])
                        {
                            sm.measured_cnt++;
                        }
                    }

                }

            }

            if (str.Equals("g_check_door\n") || str.Equals("g_check_door"))
            {
                //_check_Door();
                b_check_Door = true;
            }
            else if (str.Equals("g_ok_door\n") || str.Equals("g_ok_door"))
            {
                //_start_Process();
                b_start_Process = true;
            }
            else if(str.Equals("g_end_process") || str.Equals("g_end_process\n"))
            {
                //_endProcess();
                //sm.opticReceivedFlag = true;
            }
            else if (str.Equals("pel>Cycledone\n") || str.Equals("pel>Cycledone"))
            {
                //_endProcess();
                //_check_Door();
                //b_check_Door = true;
            }
            else if (str.Contains("LOG pel_start"))
            {
                sm.progressFirst = 5;
            }
            else if (str.Equals("PEL: JOB_READY\n") || str.Equals("PEL: JOB_READY\n"))
            {
                sm.progressFirst = 10;
            }


        }
        void SetTextBox(TextBox tb, string str)
        {
            if (this.InvokeRequired)
            {
                SetTextBoxCallback stb = new SetTextBoxCallback(SetTextBox);
                this.Invoke(stb, new object[] { tb, str });
            }
            else
            {
                tb.Text = str;
            }
        }

        public void SetCommRXProc2(string str)
        {
            str = str.Replace(" ", "");

            if (str.Equals("check_door\n"))
            {
                _check_Door();
            }
            else if (str.Equals("ok_door\n"))
            {
                _start_Process();
            }
        }


        public void SetCommRXProc3(string str)
        {
            str = str.Replace(" ", "");

            string[] commVar = str.Split('=');
            if (commVar[0].Equals("FINAL_CYCLE"))
            {
                tb_FianlCycleMCU_Eng.Text = commVar[1];
                string[] commVar2 = tb_FianlCycleMCU_Eng.Text.Split('\n');
                tb_FianlCycleMCU_Eng.Text = commVar2[0];
            }
            else if (commVar[0].Equals("OPTIC_OPERATION_KEEPING_TEMP_SEC"))
            {
                tb_OCHoldSecMCU_Eng.Text = commVar[1];
                string[] commVar2 = tb_OCHoldSecMCU_Eng.Text.Split('\n');
                tb_OCHoldSecMCU_Eng.Text = commVar2[0];
            }
            else if (commVar[0].Equals("2nd_STEP_KEEPING_TIME_SEC"))
            {
                tb_2HoldSecMCU_Eng.Text = commVar[1];
                string[] commVar2 = tb_2HoldSecMCU_Eng.Text.Split('\n');
                tb_2HoldSecMCU_Eng.Text = commVar2[0];
            }
            else if (commVar[0].Equals("DELAY_TIME_BEFORE_OPTING_RUNING"))
            {
                tb_OCDelaySecMCU_Eng.Text = commVar[1];
                string[] commVar2 = tb_OCDelaySecMCU_Eng.Text.Split('\n');
                tb_OCDelaySecMCU_Eng.Text = commVar2[0];
            }
            else if (commVar[0].Equals("PRECOND_KEEPING_TIME_MIN"))
            {
                tb_PreHoldSecMCU_Eng.Text = commVar[1];
                string[] commVar2 = tb_PreHoldSecMCU_Eng.Text.Split('\n');
                tb_PreHoldSecMCU_Eng.Text = commVar2[0];
            }
            else if (commVar[0].Equals("1st_STEP_KEEPING_TIME_SEC"))
            {
                tb_1HoldSecMCU_Eng.Text = commVar[1];
                string[] commVar2 = tb_1HoldSecMCU_Eng.Text.Split('\n');
                tb_1HoldSecMCU_Eng.Text = commVar2[0];
            }
            else if (commVar[0].Equals("1st_STEP_SETPOINT"))
            {
                tb_1TempMCU_Eng.Text = commVar[1];
                string[] commVar2 = tb_1TempMCU_Eng.Text.Split('\n');
                tb_1TempMCU_Eng.Text = commVar2[0];
            }
            else if (commVar[0].Equals("2nd_STEP_SETPOINT"))
            {
                tb_2TempMCU_Eng.Text = commVar[1];
                string[] commVar2 = tb_2TempMCU_Eng.Text.Split('\n');
                tb_2TempMCU_Eng.Text = commVar2[0];
            }
            else if (commVar[0].Equals("PRE_COND_SETPOINT"))
            {
                tb_PreTempMCU_Eng.Text = commVar[1];
                string[] commVar2 = tb_PreTempMCU_Eng.Text.Split('\n');
                tb_PreTempMCU_Eng.Text = commVar2[0];
            }
            else if (str.Equals("check_door\n") || str.Equals("check_door"))
            {
                _check_Door();
            }
            else if (str.Equals("ok_door\n") || str.Equals("ok_door"))
            {
                _start_Process();
            }
        }


        void RxPacket_CallBack(object sender)
        {
            if (sender is PcrRxProcess rx)
            {
                Pcr_Packet packet = rx.rxPacket;
                //recv_cnt++;
                //if (recv_cnt == 16)
                //{
                //    recv_cnt = 0;
                //    routine_cnt++;
                //}
                switch (packet.SubCode)
                {
                    ///////////////////////////////////////////////////////////1
                    // base 값 16개
                    case Optic_SubItemCode.SFC_OPT_BASE_1CH_TB:                             // BASE 챔버1 FAM 1
                        CH_BASE_DATA[0] = packet.data_float.ToString();
                        showLog("BASE 챔버1 FAM 수신완료");
                        //routine_cnt = 0;   // base값이 들어오기 시작함
                        break;
                    case Optic_SubItemCode.SFC_OPT_BASE_1CH_IC:                             // BASE 챔버1 ROX 2
                        CH_BASE_DATA[1] = packet.data_float.ToString();
                        showLog("BASE 챔버1 ROX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_BASE_1CH_E1D1:                          // BASE 챔버1 HEX 15
                        CH_BASE_DATA[2] = packet.data_float.ToString();
                        showLog("BASE 챔버1 HEX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_BASE_1CH_E2D2:                          // BASE 챔버1 CY5 16
                        CH_BASE_DATA[3] = packet.data_float.ToString();
                        showLog("BASE 챔버1 CY5 수신완료");
                        showLog("base 값을 모두 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_BASE_2CH_NTM:                            // BASE 챔버2 FAM 5
                        CH_BASE_DATA[4] = packet.data_float.ToString();
                        showLog("BASE 챔버2 FAM 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_BASE_2CH_IC:                             // BASE 챔버2 ROX 6
                        CH_BASE_DATA[5] = packet.data_float.ToString();
                        showLog("BASE 챔버2 ROX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_BASE_2CH_E1D1:                          // BASE 챔버2 HEX 3
                        CH_BASE_DATA[6] = packet.data_float.ToString();
                        showLog("BASE 챔버2 HEX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_BASE_2CH_E2D2:                          // BASE 챔버2 CY5 4
                        CH_BASE_DATA[7] = packet.data_float.ToString();
                        showLog("BASE 챔버2 CY5 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_BASE_3CH_E1D1:                           // BASE 챔버3 FAM 9
                        CH_BASE_DATA[8] = packet.data_float.ToString();
                        showLog("BASE 챔버3 FAM 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_BASE_3CH_E2D2:                           // BASE 챔버3 ROX 10
                        CH_BASE_DATA[9] = packet.data_float.ToString();
                        showLog("BASE 챔버3 ROX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_BASE_3CH_E1D1:                          // BASE 챔버3 HEX 7
                        CH_BASE_DATA[10] = packet.data_float.ToString();
                        showLog("BASE 챔버3 HEX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_BASE_3CH_E2D2:                          // BASE 챔버3 CY5 8
                        CH_BASE_DATA[11] = packet.data_float.ToString();
                        showLog("BASE 챔버3 CY5 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_BASE_4CH_E1D1:                           // BASE 챔버4 FAM 13
                        CH_BASE_DATA[12] = packet.data_float.ToString();
                        showLog("BASE 챔버4 FAM 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_BASE_4CH_E2D2:                           // BASE 챔버4 ROX 14
                        CH_BASE_DATA[13] = packet.data_float.ToString();
                        showLog("BASE 챔버4 ROX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_BASE_4CH_E1D1:                          // BASE 챔버4 HEX 11
                        CH_BASE_DATA[14] = packet.data_float.ToString();
                        showLog("BASE 챔버4 HEX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_BASE_4CH_E2D2:                          // BASE 챔버4 CY5 12
                        CH_BASE_DATA[15] = packet.data_float.ToString();
                        showLog("BASE 챔버4 CY5 수신완료");
                        break;
                    ///////////////////////////////////////////////////////////1
                    // cycle 값 16개
                    case Optic_SubItemCode.SFC_OPT_VAL_1CH_TB:                        //============> opt1 para1   // 15cycle 챔버1 FAM
                        ALL_DATA[0, routine_cnt] = packet.data_float.ToString();
                        showLog("챔버1 FAM 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_VAL_1CH_IC:                      //============> opt1 para2      // 15cycle 챔버1 ROX
                        ALL_DATA[1, routine_cnt ] = packet.data_float.ToString();
                        showLog("챔버1 ROX 수신완료");

                        break;
                    case Optic_SubItemCode.SFC_OPT2_VAL_1CH_E1D1:                           // 15cycle 첌버1 HEX
                        ALL_DATA[2, routine_cnt ] = packet.data_float.ToString();
                        showLog("챔버1 HEX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_VAL_1CH_E2D2:                           // 15cycle 챔버1 CY5  =========> 20사이클 마지막 step2, step4, step6
                        ALL_DATA[3, routine_cnt ] = packet.data_float.ToString();
                        showLog("챔버1 CY5 수신완료");
                        /*
                            if (routine_cnt % 2 == 1)
                            {
                                routine_cnt++;  // 최종 끝나면 routine_cnt=7 이됨 ==> 종료처리
                            }
                            */
                        break;
                    case Optic_SubItemCode.SFC_OPT_VAL_2CH_NTM:                             // 15cycle 챔버2 FAM
                        ALL_DATA[4, routine_cnt ] = packet.data_float.ToString();
                        showLog("챔버2 FAM 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_VAL_2CH_IC:                              // 15cycle 챔버2 ROX
                        ALL_DATA[5, routine_cnt ] = packet.data_float.ToString();
                        showLog("챔버2 ROX 수신완료");

                        break;

                    case Optic_SubItemCode.SFC_OPT2_VAL_2CH_E1D1:                           // 15cycle 챔버2 HEX              
                        ALL_DATA[6, routine_cnt ] = packet.data_float.ToString();
                        showLog("챔버2 HEX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_VAL_2CH_E2D2:                           // 15cycle 챔버2 CY5  =========> 15사이클 마지막  step1, step3, step5, step7
                        ALL_DATA[7, routine_cnt ] = packet.data_float.ToString();
                        showLog("챔버2 CY5 수신완료");

                        //f (routine_cnt % 2 == 0) //if (routine_cnt == 0 || routine_cnt == 2 || routine_cnt == 4 || routine_cnt == 6)
                        //{
                        //    routine_cnt++;  // 최종 끝나면 routine_cnt=7 이됨 ==> 종료처리
                        //}
                        ///if ((checkBox1.Checked && routine_cnt == 1) || routine_cnt == 45)  // 분석 프로세스가 정상적으로 종료됨  // 체크할 경우는 15사이클 후 종료
                        ///{
                        ///    showLog("분석 프로세스가 정상 종료됨");
                        ///    _endProcess();
                        ///}
                        break;
                    case Optic_SubItemCode.SFC_OPT_VAL_3CH_E1D1:                            // 15cycle 챔버3 FAM
                        ALL_DATA[8, routine_cnt ] = packet.data_float.ToString();
                        showLog("챔버3 FAM 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_VAL_3CH_E2D2:                            // 15cycle 챔버3 ROX
                        ALL_DATA[9, routine_cnt ] = packet.data_float.ToString();
                        showLog("챔버3 ROX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_VAL_3CH_E1D1:                   //============> opt2 para1    // 15cycle 챔버3 HEX
                        ALL_DATA[10, routine_cnt ] = packet.data_float.ToString();
                        showLog("챔버3 HEX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_VAL_3CH_E2D2:
                        ALL_DATA[11, routine_cnt ] = packet.data_float.ToString();      //============> opt2 para2     // 15cycle 챔버3 CY5
                        showLog("챔버3 CY5 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_VAL_4CH_E1D1:                            // 15cycle 챔버4 FAM
                        ALL_DATA[12, routine_cnt ] = packet.data_float.ToString();
                        showLog("챔버4 FAM 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_VAL_4CH_E2D2:                            // 15cycle 챔버4 ROX
                        ALL_DATA[13, routine_cnt ] = packet.data_float.ToString();
                        showLog("챔버4 ROX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_VAL_4CH_E1D1:                           // 15cycle 챔버4 HEX
                        ALL_DATA[14, routine_cnt ] = packet.data_float.ToString();
                        showLog("챔버4 HEX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_VAL_4CH_E2D2:                           // 15cycel 챔버4 CY5
                        ALL_DATA[15, routine_cnt ] = packet.data_float.ToString();
                        showLog("챔버4 VY5 수신완료");
                        break;
                    ///////////////////////////////////////////////////////////1
                    case Optic_SubItemCode.SFC_OPT_TEST:
                        Console.WriteLine("{0}", packet.ToString());
                        break;
                }


            }
        }

        private void _endProcess()
        {
            /////////////////////////////////////////////////////
            /// 정상적인 프로세스 종료에 따른 후속 작업
            ///
            updateDeviceStatus("Stop");

            endTestInfo();
            update_analytic_result();//updateAnalyticResult(selectedDgv);
            update_IC_result();


            update_testResult_final();


            _reset_Process();

            //last data update 
            sm.DataUpdateFlag = true;
            this.tabControl_Tester.SelectTab(0);

            MessageBox.Show("검사가 끝났습니다. 결과를 확인해 주세요.", "검사 종료 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);

            

            // 결과 인쇄(pdf)
            //btn_Print_Click_1(null, null);

            // 관련 변수들 초기화




        }

        enum RECORD_IDX
        {
            DATE = 0, MC_NO = 1, TEST_NAME = 2, START_TIME = 3, END_TIME = 4, CARTRIDGE_ID = 5, SAMPLE_ID = 6, RESULT = 7, FILENAME = 8, USER_NAME = 9, USER_ID = 10, CT = 11,

        }

        private void printDocument1_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;

            // 배경 이미지를 인쇄
            //System.Drawing.Image img = System.Drawing.Image.FromFile("data\\ABI출력폼.bmp");

            System.Drawing.Image img = System.Drawing.Image.FromFile("data\\TestReport.bmp");

            Point loc = new Point(0, 0);
            e.Graphics.DrawImage(img, loc);


            float x_org = 105.0F;
            float y_org = 95.0F;

            //sm.RecordInfoArr[0] = dgv_test_records.Rows[index].Cells[0].FormattedValue.ToString(); // Date
            //sm.RecordInfoArr[1] = dgv_test_records.Rows[index].Cells[1].FormattedValue.ToString(); // Machine Number
            //sm.RecordInfoArr[2] = dgv_test_records.Rows[index].Cells[2].FormattedValue.ToString(); // TestName
            //sm.RecordInfoArr[3] = dgv_test_records.Rows[index].Cells[3].FormattedValue.ToString(); // StartTime
            //sm.RecordInfoArr[4] = dgv_test_records.Rows[index].Cells[4].FormattedValue.ToString(); // EndTime
            //sm.RecordInfoArr[5] = dgv_test_records.Rows[index].Cells[5].FormattedValue.ToString(); // Cartridge ID
            //sm.RecordInfoArr[6] = dgv_test_records.Rows[index].Cells[6].FormattedValue.ToString(); // Sample ID
            //sm.RecordInfoArr[7] = dgv_test_records.Rows[index].Cells[7].FormattedValue.ToString(); // Result
            //sm.RecordInfoArr[8] = dgv_test_records.Rows[index].Cells[8].FormattedValue.ToString(); // fileName 


            //test Name 
            myStringPrint(g, sm.RecordInfoArr[(int)RECORD_IDX.TEST_NAME], x_org, y_org);

            //Start time
            if (sm.StartTime == null)
            {
                //sm.StartTime = DateTime.Now.ToString();
                myStringPrint(g, sm.RecordInfoArr[(int)RECORD_IDX.START_TIME], x_org, y_org + 32);
            }
            else
            {
                myStringPrint(g, sm.RecordInfoArr[(int)RECORD_IDX.START_TIME], x_org, y_org + 32);
            }

            //End Time
            if (sm.EndTime == null)
            {
                myStringPrint(g, sm.RecordInfoArr[(int)RECORD_IDX.END_TIME], x_org, y_org + 64);
            }
            else
            {
                myStringPrint(g, sm.RecordInfoArr[(int)RECORD_IDX.END_TIME], x_org, y_org + 64);
            }

            //tester Name
            myStringPrint(g, sm.RecordInfoArr[(int)RECORD_IDX.USER_NAME], x_org + 15, y_org + 160);

            //tester ID
            myStringPrint(g, sm.RecordInfoArr[(int)RECORD_IDX.USER_ID], x_org + 15, y_org + 192);

            //Sample ID 
            myStringPrint(g, sm.RecordInfoArr[(int)RECORD_IDX.SAMPLE_ID], x_org + 15, y_org + 292);

            //CartridgeID
            myStringPrint(g, sm.RecordInfoArr[(int)RECORD_IDX.CARTRIDGE_ID], x_org + 15, y_org + 324);

            //Result
            myStringPrint(g, sm.RecordInfoArr[(int)RECORD_IDX.RESULT], x_org + 115, y_org + 440);

            //CT Values
            myStringPrint(g, sm.RecordInfoArr[(int)RECORD_IDX.CT], x_org + 55, y_org + 540);
            myStringPrint(g, sm.RecordInfoArr[12], x_org + 55, y_org + 562);
            myStringPrint(g, sm.RecordInfoArr[13], x_org + 55, y_org + 584);
            myStringPrint(g, sm.RecordInfoArr[14], x_org + 55, y_org + 606);

            myStringPrint(g, sm.RecordInfoArr[15], x_org + 170, y_org + 540);
            myStringPrint(g, sm.RecordInfoArr[16], x_org + 170, y_org + 562);
            myStringPrint(g, sm.RecordInfoArr[17], x_org + 170, y_org + 584);
            myStringPrint(g, sm.RecordInfoArr[18], x_org + 170, y_org + 606);

            myStringPrint(g, sm.RecordInfoArr[19], x_org + 285, y_org + 540);
            myStringPrint(g, sm.RecordInfoArr[20], x_org + 285, y_org + 562);
            myStringPrint(g, sm.RecordInfoArr[21], x_org + 285, y_org + 584);
            myStringPrint(g, sm.RecordInfoArr[22], x_org + 285, y_org + 606);

            myStringPrint(g, sm.RecordInfoArr[23], x_org + 400, y_org + 540);
            myStringPrint(g, sm.RecordInfoArr[24], x_org + 400, y_org + 562);
            myStringPrint(g, sm.RecordInfoArr[25], x_org + 400, y_org + 584);
            myStringPrint(g, sm.RecordInfoArr[26], x_org + 400, y_org + 606);
        }


        private void myStringPrint(Graphics g, string str, float x, float y)
        {

            PointF drawPoint = new PointF(x, y);
            using (Font font = new Font("문체부 제목 돋음체", 12))
            //using (SolidBrush drawBrush = new SolidBrush(Color.Black))
            using (SolidBrush drawBrush = new SolidBrush(Color.DarkGray))
            {
                g.DrawString(str, font, drawBrush, drawPoint);
            }

        }


        private void btn_Print_Click_1(object sender, EventArgs e)
        {
            string dt = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");


            // 결과 pdf 인쇄
            /*
            string filePath = Application.StartupPath + @"\report";
            filePath += "/PCR " + dt + ".pdf";

            printDocument1.DocumentName = "PCR report printing...";
            printDocument1.PrinterSettings.PrinterName = "Microsoft Print to PDF";
            printDocument1.PrinterSettings.PrintFileName = filePath;
            printDocument1.PrinterSettings.PrintToFile = true;
            printDocument1.Print(); // 인쇄 시작
            */

            // 결과 csv 저장
            string filePath2 = Application.StartupPath + @"\log";
            
            filePath2 += "/PCR " + dt + ".csv";

            Save_GridCsv(filePath2);
        }

        private void tb_Data_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnSetBaseData_Click(object sender, EventArgs e)
        {
            setGridValue(0);
        }

        private void btnSet15Data_Click(object sender, EventArgs e)
        {
            setGridValue(1);
        }

        private void btnSet20Data_Click(object sender, EventArgs e)
        {
            setGridValue(2);
        }

        private void btnSet25Data_Click(object sender, EventArgs e)
        {
            setGridValue(3);
        }

        private void btnSet30Data_Click(object sender, EventArgs e)
        {
            setGridValue(4);
        }

        private void btnSet35Data_Click(object sender, EventArgs e)
        {
            setGridValue(5);
        }

        private void btnSet40Data_Click(object sender, EventArgs e)
        {
            setGridValue(6);
        }

        private void btnSetFinalData_Click(object sender, EventArgs e)
        {
            setGridValue(7);
        }

        private void btn_DoorOpen_Test_Click(object sender, EventArgs e)
        {
            // 검사 준비 중의 경우에는 검사를 초기화 할지 확인
            if (processStep > 0)
            {
                // 확인
                if (MessageBox.Show("문을 엽니다. \n검사 준비를 초기화하려면 [예], \n검사를 계속 하려면 [아니오]를 누르세요.", "문열림 안내", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    // 검사 초기화
                    setProcessMode(0);
                }
                else   // 문만 열었다가 계속 검사할 경우
                {
                }
            }

            // 검사창 도어 오픈
            StringBuilder sb = new StringBuilder();
            sb.Append(":DOOR_UNLOCK");

            String txt = sb.ToString();
            serial.SendLine(txt);
        }

        private void MainFrm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 검사 중이면 검사를 종료해야 종료 가능
            if (processStep == 9)
            {
                MessageBox.Show("검사 중에는 프로그램을 종료할 수 없습니다.", "종료 안내", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;

                return;
            }

            // 종료 확인
            if (MessageBox.Show("프로그램을 종료하시겠습니까?", "종료 안내", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
            {
                // 종료 
                e.Cancel = false;
                

                foreach (Process process in Process.GetProcesses())
                {
                    //프로그램명으로 시작되는 프로세스를 모두 죽인다. 엉뚱한 프로세스를 죽이지 않게 IF문을 잘 사용한다.
                    if (process.ProcessName.ToUpper().StartsWith("ABI_POC_PCR"))
                    {
                        process.Kill();
                    }
                }
                
            }
            else   // 취소
            {
                e.Cancel = true;
            }
        }

        private void seqTestBtn_Click(object sender, EventArgs e)
        {
            //serial.SendLine( mm.moveSM(1, 0, 500, 800) );
            mm.moveSM(1, 0, 500, 800);
        }

        private void tabPage_tester_Click()
        {
            //test info. list view 
            lv_testInfo.View = View.Details;           //컬럼형식으로 변경

            lv_testInfo.FullRowSelect = true;          //Row 전체 선택

            lv_testInfo.Columns.Add("Test Name", 433);
            lv_testInfo.Columns.Add("Start Time", 433);        //컬럼추가
            lv_testInfo.Columns.Add("End Time ", 433);

            //test 
            lv_testerInfo.View = View.Details;           //컬럼형식으로 변경
            lv_testerInfo.FullRowSelect = true;          //Row 전체 선택

            lv_testerInfo.Columns.Add("Tester Name", 250);
            lv_testerInfo.Columns.Add("Tester ID ", 250);        //컬럼추가
                

            //test 
            listView_PatientInfo.View = View.Details;           //컬럼형식으로 변경
            listView_PatientInfo.FullRowSelect = true;          //Row 전체 선택

            listView_PatientInfo.Columns.Add("Patient ID", 250);
            listView_PatientInfo.Columns.Add("Sample ID ", 250);        //컬럼추가
            listView_PatientInfo.Columns.Add("Cartridge ID ", 250);        //컬럼추가

    
            //listView_Result.Columns.Add("N T M", 433);        //컬럼추가
            //listView_Result.Columns.Add("R I F", 433);  

            //listView_Result.Columns.Add("COVID", 433);
            //listView_Result.Columns.Add("N T M", 433);        //컬럼추가
            //listView_Result.Columns.Add("R I F", 433);        //컬럼추가
            //reset_AnalyticResult_COVID();                                                  //listView_Result.Columns.Add("I N H", 250);        //컬럼추가
            ////////////////////////////////////////////
       
            
            //updatePatientInfo();
        }
        private void tabPage_tester_Click(object sender, EventArgs e)
        {

        }

        private void tabPage_Result_Click(object sender, EventArgs e)
        {


        }

        private void initTesterInfo()
        {
            lv_testerInfo.BeginUpdate();
            ListViewItem lvi2 = new ListViewItem(sm.userName);
            lvi2.SubItems.Add(sm.userID);
            lvi2.SubItems[0].BackColor = Color.WhiteSmoke;
            //lvi2.SubItems.Add("임시1");
            //lvi2.SubItems.Add("인천광역시 부평구");
            lvi2.ImageIndex = 0;
            lv_testerInfo.Items.Add(lvi2);
            lv_testerInfo.EndUpdate();
            //*************************************************************         
        }

        private void updateTesterInfo()
        {
            lv_testerInfo.BeginUpdate();

            sm.userName = tb_WorkerName_Test.Text;
            sm.userID = tb_WorkerID_Test.Text;

            lv_testerInfo.Items[0].SubItems[0].Text = tb_WorkerName_Test.Text;
            lv_testerInfo.Items[0].SubItems[1].Text = tb_WorkerID_Test.Text;
            lv_testerInfo.EndUpdate();
        }

        private void resetTesterInfo()
        {
            /*
            listView_inspectorInfo.BeginUpdate();
            listView_inspectorInfo.Items[0].SubItems[0].Text = "";
            listView_inspectorInfo.Items[0].SubItems[1].Text = "";
            listView_inspectorInfo.EndUpdate();
            */
        }
        
        public void initTestInfo()
        {
            lv_testInfo.BeginUpdate();

            lvi1 = new ListViewItem("");
            lvi1.SubItems.Add("");
            lvi1.SubItems.Add("");

            lvi1.SubItems[0].BackColor = Color.WhiteSmoke;

            lvi1.ImageIndex = 0;
            lv_testInfo.Items.Add(lvi1);

            lv_testInfo.EndUpdate();
        }

        public void updateTestInfo()
        {
            test_start_time = DateTime.Now.ToString("HH:mm");
            sm.StartTime = test_start_time;
            lv_testInfo.BeginUpdate();
            lv_testInfo.Items[0].SubItems[0].Text = sm.testName;
            lv_testInfo.Items[0].SubItems[1].Text = test_start_time;
            lv_testInfo.Items[0].SubItems[2].Text = "";
          
            //listView_ReportInfo.Items.Add(lvi1);
            lv_testInfo.EndUpdate();
        }

        public void endTestInfo()
        {
            test_end_time = DateTime.Now.ToString("HH:mm");
            sm.EndTime = test_end_time;
            lv_testInfo.BeginUpdate();

            //lvi1 = new ListViewItem("TB");
            lv_testInfo.Items[0].SubItems[2].Text = test_end_time;
            //listView_ReportInfo.Items.Add(lvi1);

            lv_testInfo.EndUpdate();
        }


        public int getRandNumber()
        {
            int random_number = r.Next(1, 100);
            return random_number;
        }


       

        public string[] CriticalThresholdCalculation(string[] simul_value)
        {
            //Ct calculation
            //string[] value_Dye = new string[26];
            // { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };

            //string[] simul_value = new string[26]{ "8", "8", "9", "10", "8", "10", "10", "11", "9", "8", "11", "20", "30", "70", "90", "100", "105", "106", "107", "108", "109", "109", "108", "107", "107", "107" };

            /*
            Random r = new Random();
            int random_number = 0;
            for (int i = 0; i < 26; i++)
            {
                //double number = Math.Pow(2, i);
                random_number = r.Next(1, 100);
                value_Dye[i] = random_number.ToString();
            }
            */

            double[] value_Dye_Slope = new double[25] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, };
            double[] second_Slope = new double[24] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, };

            double tempMax = 0;
            int tempMax_Index = 0;

            /*
            for (int i = 0; i < 26; i++)
            {
                value_Dye[i] = dataGridView5.Rows[0].Cells[i].FormattedValue.ToString();
            }
            */

            for (int j = 0; j < 25; j++)
            {
                value_Dye_Slope[j] = Convert.ToDouble(simul_value[j + 1]) - Convert.ToDouble(simul_value[j]);
                //value_Dye_Slope[j] = Convert.ToDouble(value_Dye[j + 1]) - Convert.ToDouble(value_Dye[j]);
            }
            for (int j = 1; j < 24; j++)
            {
                second_Slope[j] = Math.Abs(value_Dye_Slope[j] - value_Dye_Slope[j - 1]);
                if (Math.Abs(second_Slope[j] - second_Slope[j - 1]) > tempMax)
                {
                    tempMax = second_Slope[j];
                    sm.criticalThreshold = j;
                }
            }

            string[] array = new string[2];
            array[0] = ((sm.criticalThreshold).ToString());
            array[1] = tempMax.ToString();
            return array;
            //MessageBox.Show((sm.criticalThreshold).ToString());

        }

        public int isExceededBase_multiply(DataGridView dgv, int RowIndex, int maxIndex, int compareVal)
        {
            return 0;
        }

        public int isExceededBase_sigma(DataGridView dgv, int RowIndex, int maxIndex, int compareVal)
        {
            return 0;
        }

        public double isExceededBase_userSelection(DataGridView dgv, int RowIndex, int maxIndex, double compareVal) // if false  return -1;
        {
            try
            {
                string[] array;
                array = new string[maxIndex];
                int opticVal = 0;
                int base_optic_Val = 0;
                for (int i = 10; i < maxIndex +10 ; i++)
                {
                    //base_optic_Val = 5;
                    array[i] = dgv.Rows[RowIndex].Cells[i + 1].FormattedValue.ToString();
                    Int32.TryParse(dgv.Rows[RowIndex].Cells[1].FormattedValue.ToString(), out base_optic_Val);
                    Int32.TryParse(array[i], out opticVal);

                    if (opticVal > compareVal)//if (opticVal > base_optic_Val * 1.2)
                    {
                        return compareVal;// (int)(base_optic_Val * 1.2);
                    }
                }
                

            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return -1;
        }

        
        public void update_IC_result()
        {
            //listViewResultIC.BeginUpdate();
            //listViewResultIC.EndUpdate();
           //if( value_ExceededBase(dataGridView5, 2, 28) == -1 )
           //{
           //    lb_IC_Result.Text = "Fail";
           //}
           //else if( value_ExceededBase(dataGridView5, 2, 28) > 0)
           //{
           //    lb_IC_Result.Text = "PASS";
           //}
        }
        
        public void initializeAnalyticResult()
        {
           
        }

        public void resetCartridgeInfo()
        {
            listView_PatientInfo.BeginUpdate();
            listView_PatientInfo.Items[0].SubItems[0].Text = "";
            listView_PatientInfo.Items[0].SubItems[1].Text = "";
            listView_PatientInfo.Items[0].SubItems[2].Text = "";
            listView_PatientInfo.EndUpdate();
        }

        public void initCartridgeInfo()
        {
            listView_PatientInfo.BeginUpdate();
            lvi3 = new ListViewItem("");
            lvi3.SubItems.Add("");
            lvi3.SubItems.Add("");
            lvi3.SubItems[0].BackColor = Color.WhiteSmoke;
            //lvi2.SubItems.Add("임시1");
            //lvi2.SubItems.Add("인천광역시 부평구");
            lvi3.ImageIndex = 0;
            listView_PatientInfo.Items.Add(lvi3);
            listView_PatientInfo.EndUpdate();
        }

        public void updateCartridgeInfo()
        {
            sm.PatientID = tb_PatientID_Test.Text;
            sm.SampleID = tb_SampleID_Test.Text;
            sm.CartridgeID = tb_CartridgeID_Test.Text;

            listView_PatientInfo.BeginUpdate();
            listView_PatientInfo.Items[0].SubItems[0].Text = tb_PatientID_Test.Text;
            listView_PatientInfo.Items[0].SubItems[1].Text = tb_SampleID_Test.Text;
            listView_PatientInfo.Items[0].SubItems[2].Text = tb_CartridgeID_Test.Text;
            listView_PatientInfo.EndUpdate();
        }

        public string[] getDataGridArray(DataGridView dgv,int RowIndex, int maxIndex)
        {
            string[] array;
            array = new string[maxIndex];
            for(int i = 0; i < maxIndex; i++)
            {
                array[i] = dgv.Rows[RowIndex].Cells[i + 1].FormattedValue.ToString();
            }
            return array;
        }

        private void testButton_Click(object sender, EventArgs e)
        {
            

            //updateAnalyticResult();
            //UpdateICResult();

            //CriticalThresholdCalculation Test
            /*
                string[] result = CriticalThresholdCalculation(getDataGridArray(dataGridView5, 1, 28));
                lb_IC_Result.Text = result[0] + "* Value : " + result[1];

                int tempValue = 0;
                Int32.TryParse(result[1], out tempValue);
                Plotter.DrawHLine(formsPlot1, tempValue);
            */

            double IC_value = isExceededBase_userSelection(dgv_opticDatum_tube1, 1, 10, 2500);
            Plotter.DrawHLine(formsPlot1, (int)IC_value);


            //string[] array = getDataGridArray(dataGridView5, 1, 28);
            //CriticalThresholdCalculation(array);
            //MessageBox.Show(dataGridView5.Rows[0].Cells[1].FormattedValue.ToString());
            //CriticalThresholdCalculation();

            //CriticalThresholdCalculation();
            MessageBox.Show(dgv_opticDatum_tube1.Rows[0].Cells[1].FormattedValue.ToString());
            //MessageBox.Show(dataGridView_Diagnosis.Rows[0].Cells[1].FormattedValue.ToString());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //update test infomation
            test_start_time = DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");

            lv_testInfo.BeginUpdate();

            lvi1 = new ListViewItem("COVID");
            lvi1.SubItems.Add(test_start_time);
            lvi1.SubItems.Add("");

            lvi1.SubItems[0].BackColor = Color.WhiteSmoke;

            lvi1.ImageIndex = 0;
            lv_testInfo.Items.Add(lvi1);

            lv_testInfo.EndUpdate();
        }

        private void btn_Connect_Barcode_Click(object sender, EventArgs e)
        {
            // 컴포트 선택 후 연결 버튼 클릭하면 ID, PW 입력창이 활성화됨
            //showLog("장치와 연결 동작");

            //컴포트 연결시도
            bool flag = Open_exec_barcode();

            //연결에 성공하면
            if (flag)
            {
               
                btn_Connect_Barcode.Text = "Disconnect";
            }
            else
            {
               
                btn_Connect_Barcode.Text = "Connect";
            }
            //연결에 성공하면

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void formsPlot1_Load(object sender, EventArgs e)
        {

        }

       
        delegate void Ctr_Involk(Control ctr, String text);
        public void setText_Control_Invoke(Control ctr, string txtValue)
        {
            if (ctr.InvokeRequired)
            {
                Ctr_Involk Cl = new Ctr_Involk(setText_Control_Invoke);
                ctr.Invoke(Cl, ctr, txtValue);
            }
            else
            {
                ctr.Text = txtValue;
            }
        }

        delegate void Ctr_Invoke_Integer(Control ctr, int integer);
        public void setInteger_Control_Invoke(Control ctr, int number)
        {
            if(ctr.InvokeRequired)
            {
                Ctr_Invoke_Integer Cl = new Ctr_Invoke_Integer(setInteger_Control_Invoke);
                ctr.Invoke(Cl, ctr, number);
            }
        }

        public void presetGrid_Diagnosis()
        {
            string[] data1 = new string[] { };
            string[] data2 = new string[] { };
        }

        private void btn_RT_PreTemp_Eng_Click(object sender, EventArgs e)
        {
            if (tb_RT_PreTemp_Eng.Text != "" && tb_RT_PreTemp_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":RT_PRE_COND_SETPOINT");
                sb.Append(" ");
                sb.Append(tb_RT_PreTemp_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        private void btn_RT_PreHoldSec_Eng_Click(object sender, EventArgs e)
        {
            if (tb_RT_PreHoldSec_Eng.Text != "" && tb_RT_PreHoldSec_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":RT_PRECOND_KEEPING_TIME_MIN");
                sb.Append(" ");
                sb.Append(tb_RT_PreHoldSec_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        private void btn_MachineNum_Eng_Click(object sender, EventArgs e)
        {
            if (tb_MachineNum_Eng != null && tb_MachineNum_Eng.Text != "" && tb_MachineNum_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":MACHINE_NUM");
                sb.Append(" ");
                sb.Append(tb_MachineNum_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        private void btn_OpticCurrentFAM_Click(object sender, EventArgs e)
        {
            if (tb_OpticCurrentFAM != null && tb_OpticCurrentFAM.Text != "" && tb_OpticCurrentFAM.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":OPTIC_CURRENT_FAM");
                sb.Append(" ");
                sb.Append(tb_OpticCurrentFAM.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        private void btn_OpticCurrentROX_Click(object sender, EventArgs e)
        {
            if (tb_OpticCurrentROX != null && tb_OpticCurrentROX.Text != "" && tb_OpticCurrentROX.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":OPTIC_CURRENT_ROX");
                sb.Append(" ");
                sb.Append(tb_OpticCurrentROX.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        private void btn_OpticCurrentHEX_Click(object sender, EventArgs e)
        {
            if (tb_OpticCurrentHEX != null && tb_OpticCurrentHEX.Text != "" && tb_OpticCurrentHEX.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":OPTIC_CURRENT_HEX");
                sb.Append(" ");
                sb.Append(tb_OpticCurrentHEX.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        private void btn_OpticCurrentCY5_Click(object sender, EventArgs e)
        {
            if (tb_OpticCurrentCY5 != null && tb_OpticCurrentCY5.Text != "" && tb_OpticCurrentCY5.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":OPTIC_CURRENT_CY5");
                sb.Append(" ");
                sb.Append(tb_OpticCurrentCY5.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }


        private void btn_PreTempMCU_Eng_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            processStep = 9;
            //sm.Routine_Cnt = 7;
            sm.measured_cnt = 0;

            updateTestInfo();
            resetTesterInfo();
            resetCartridgeInfo();
        

            presetDataGrid_ct(dgv_opticDatum_tube1, chamberName[0]);
            presetDataGrid_ct(dgv_opticDatum_tube2, chamberName[1]);
            presetDataGrid_ct(dgv_opticDatum_tube3, chamberName[2]);
            presetDataGrid_ct(dgv_opticDatum_tube4, chamberName[3]);

            /*
            int rand = 0;
            for (int i = 0; i < 16; i++)
            {
                rand = r.Next(500, 2500);
                CH_BASE_DATA[i] = rand.ToString();
                ///MEASURED_DATA[i, sm.measured_cnt] = CH_BASE_DATA[i];
            }
            */

        }

        private void btnSimul2_Click(object sender, EventArgs e)
        {
            sm.Routine_Cnt = 15;
         }

        private void btnSimul3_Click(object sender, EventArgs e)
        {
            sm.Routine_Cnt++;

            for (int i = 0; i < Plotter.COL_CNT; i++)
            {
                if (sm.Routine_Cnt == Plotter.Optic_Measure_Idx[i])
                {
                    sm.measured_cnt++;
                }
            }
            /*        
            int rand = 0;
            for (int i = 0; i< 16; i++)
            {
                rand = r.Next(500, 2500);
                ALL_DATA[i, routine_cnt ] = rand.ToString();
                
            }
            */
        }

        private void btnSimul4_Click(object sender, EventArgs e)
        {
            _endProcess();
            //sm.Routine_Cnt = 45;
            /*
            int rand = 0;
            for (int i = 0; i < 16; i++)
            {
                rand = r.Next(500, 2500);
                ALL_DATA[i, routine_cnt ] = rand.ToString();
                //MEASURED_DATA[i, sm.measured_cnt] = ALL_DATA[i, routine_cnt - 1];
            }
            */
        }

        private void btnGetPorts_Click(object sender, EventArgs e)
        {
            for(int i = 0; i < cb_Port_Main.Items.Count; i++)
            {
                cb_Port_Main.Items.RemoveAt(0);
            }
            string[] comlist = System.IO.Ports.SerialPort.GetPortNames();
            //COM Port가 있는 경우에만 콤보 박스에 추가.
            if (comlist.Length > 0)
            {
                cb_Port_Main.Items.AddRange(comlist);
                //제일 처음에 위치한 녀석을 선택
                cb_Port_Main.SelectedIndex = 0;
            }
        }

        private void btnBarcodeGetPorts_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < cb_Port_Barcode.Items.Count; i++)
            {
                cb_Port_Barcode.Items.RemoveAt(0);
            }


            //cb_Port_Barcode.Refresh();
            string[] comlist = System.IO.Ports.SerialPort.GetPortNames();

            //COM Port가 있는 경우에만 콤보 박스에 추가.
            if (comlist.Length > 0)
            {
                cb_Port_Barcode.Items.AddRange(comlist);
                //제일 처음에 위치한 녀석을 선택
                cb_Port_Barcode.SelectedIndex = 0;
            }
        }

        private void btn_firstGraph_Click(object sender, EventArgs e)
        {
            
            MatchAndFindOpticDataForResult();
            sm.opticReceivedFlag = true;
            setGridValue(7);
        }

        private void btnGraph_Click(object sender, EventArgs e)
        {            
            Plotter.ResetAllPlots(formsPlot1, formsPlot2, formsPlot3, formsPlot4);
            MatchAndFindOpticDataForResult();
            sm.opticReceivedFlag = true;
            setGridValue(7);
        }

        private void btnLogFileRead_Click(object sender, EventArgs e)
        {
            logToFile.MakeNewFile();
            //MatchAndFindOpticDataForResult();
            //sm.opticReceivedFlag = true;
            //setGridValue(7);
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            //sm.opticReceivedFlag = true;
            //setGridValue(7);
            _reset_Process();
        }

        private void cb_Diagnosis_Target_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(cb_Test_Interpretation.SelectedIndex == 0)
            {
                dgv_interpretation_howTo.Visible = true;
            
                selectedDgv = dgv_interpretation_howTo;
            }
            else if(cb_Test_Interpretation.SelectedIndex == 1)
            {
                dgv_interpretation_howTo.Visible = false;
             
            }
            else if(cb_Test_Interpretation.SelectedIndex == 2)
            {
                dgv_interpretation_howTo.Visible = false;
              
            }
            
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            setCtValueToDGV(dgv_interpretation_ct, Plotter.CtCycles);
            setCtResultToDGV(dgv_interpretation_ct);
            update_analytic_result();

            update_testResult_final();
            //updateAnalyticResult(selectedDgv);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //presetDataGrid_ct(dataGridView5, chamberName[0]);
            //presetDataGrid_ct(dataGridView6, chamberName[1]);
            //presetDataGrid_ct(dataGridView7, chamberName[2]);
            //presetDataGrid_ct(dataGridView8, chamberName[3]);
            presetDataGrid_ct_randomly(dgv_opticDatum_tube1, chamberName[0]);
            presetDataGrid_ct_randomly(dgv_opticDatum_tube2, chamberName[1]);
            presetDataGrid_ct_randomly(dgv_opticDatum_tube3, chamberName[2]);
            presetDataGrid_ct_randomly(dgv_opticDatum_tube4, chamberName[3]);
        }

        private void btnDiagnosisLoad_Click(object sender, EventArgs e)
        {
            //OpenFileDialog dlg = new OpenFileDialog();
            //dlg.OpenFile(); 
          
         
        }

        private void btnDiagnosisNew_Click(object sender, EventArgs e)
        {
                      
        }

        private void btnDiagnosisSave_Click(object sender, EventArgs e)
        {

        }

        private void btnDiagnosisDelete_Click(object sender, EventArgs e)
        {

        }
        private void SetText(TextBox txtbox, string text)
        {
            txtbox.Text = text;
        }
        private void btnDiagnosisFindAndLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var sr = new StreamReader(openFileDialog1.FileName);
                    SetText(textBox_ExcelFilePath, openFileDialog1.FileName);
                    //SetText(textBox_ExcelFilePath ,sr.ReadToEnd());

                    //OpenFile(textBox_ExcelFilePath.Text);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    //MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    //$"Details:\n\n{ex.StackTrace}");
                }
            }

        }

        private void button4_Click(object sender, EventArgs e)
        {
            DataGridView[] dgvArr = new DataGridView[4] { dgv_opticDatum_tube1, dgv_opticDatum_tube2, dgv_opticDatum_tube3, dgv_opticDatum_tube4 };
            MatchAndFindOpticDataForResult();
            //updateDataGridOpticDatum(dgvArr, DISPLAY_DATA);
           
            /*
            DataGridView[] dgvArr = new DataGridView[4] { dgv_opticDatum_tube1, dgv_opticDatum_tube2, dgv_opticDatum_tube3, dgv_opticDatum_tube4 };
            updateDataGridOpticDatum(dgvArr, MEASURED_DATA);

            int[] ic_value = new int[16];
            for (int i = 0; i < 16; i++)
            {
                Int32.TryParse(MEASURED_DATA[i, 0], out ic_value[i]);
                string ct_temp = dgv_diagnosis_ct.Rows[i].Cells[1].FormattedValue.ToString();
                int iCt_temp = 0;
                Int32.TryParse(ct_temp, out iCt_temp);

                ic_value[i] = (int)( iCt_temp * ic_value[i]);
                //ic_value[i] = MEASURED_DATA[i, 0];
            }

            for (int i = 0; i < CH_CNT; i++)
            {
                updateScottPlot(i, ic_value);
            }
            */
        }

        public void removeAlldgv()
        {
            DataGridView[] dgvArr = new DataGridView[4] { dgv_opticDatum_tube1, dgv_opticDatum_tube2, dgv_opticDatum_tube3, dgv_opticDatum_tube4 };

            try
            {
                for (int j = 0; j < Plotter.DYE_CNT; j++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (dgvArr[j].Rows.GetRowCount(DataGridViewElementStates.Visible) != 0)
                        {
                            dgvArr[j].Rows.RemoveAt(0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);

            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            

           
        }

        private void button6_Click(object sender, EventArgs e)
        {
            removeAlldgv();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            initAllScottPlot();

            //Plotter.ResetAllPlots(formsPlot1, formsPlot2, formsPlot3, formsPlot4);
        }

        public void initAllScottPlot()
        {
            Plotter.ViewInit(formsPlot1);
            Plotter.ViewInit(formsPlot2);
            Plotter.ViewInit(formsPlot3);
            Plotter.ViewInit(formsPlot4);

            Plotter.ch1DataDic["FAM"] = new List<double>();
            Plotter.ch1DataDic["ROX"] = new List<double>();
            Plotter.ch1DataDic["HEX"] = new List<double>();
            Plotter.ch1DataDic["CY5"] = new List<double>();

            Plotter.ch2DataDic["FAM"] = new List<double>();
            Plotter.ch2DataDic["ROX"] = new List<double>();
            Plotter.ch2DataDic["HEX"] = new List<double>();
            Plotter.ch2DataDic["CY5"] = new List<double>();

            Plotter.ch3DataDic["FAM"] = new List<double>();
            Plotter.ch3DataDic["ROX"] = new List<double>();
            Plotter.ch3DataDic["HEX"] = new List<double>();
            Plotter.ch3DataDic["CY5"] = new List<double>();

            Plotter.ch4DataDic["FAM"] = new List<double>();
            Plotter.ch4DataDic["ROX"] = new List<double>();
            Plotter.ch4DataDic["HEX"] = new List<double>();
            Plotter.ch4DataDic["CY5"] = new List<double>();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            baseCalculationAverage();//baseCalculation();
        }

        private void cb_Recipe_Test_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(cb_Recipe_Test.SelectedText == cb_Recipe_Eng.SelectedText 
                && cb_Recipe_Test.SelectedText == cb_Test_Interpretation.SelectedText )
            {
                //cb_Recipe_Test.Text = sm.testName;
                //pass
            }
            else
            {
                MessageBox.Show("Start Test와 Interpretation 메뉴를 다시 검토해 주세요 .", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    //var sr = new StreamReader(openFileDialog1.FileName);
                    SetText(textBox1, openFileDialog1.FileName);
                    sm.currentLogFileName = openFileDialog1.FileName;
                    //SetText(textBox_ExcelFilePath ,sr.ReadToEnd());

                    //OpenFile(textBox_ExcelFilePath.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            processStep = 4;
            if (processStep > 2)
            {
                gb_CartridgeInfo.Enabled = true;
                gb_TestInfo.Enabled = true;
            }


        }

        private void button11_Click(object sender, EventArgs e)
        {
            sm.measured_cnt = Plotter.COL_CNT;
            updateAllPlots();
        }

        public void updateAllPlots()
        {
            try
            {
                Plotter.ResetAllPlots(formsPlot1, formsPlot2, formsPlot3, formsPlot4);
                for (int i = 0; i < Plotter.CH_CNT; i++)
                {
                    if (chkBox_baselineNoScale.Checked || chkBox_BaselineScale.Checked)
                    {
                        for (int j = 0; j < Plotter.CH_CNT * Plotter.DYE_CNT; j++)
                        {
                            zerosetValArray[j] = (CtlineVal[j] - BaselineVal[j]) * scaleFactor[j];
                        }
                        updateScottPlot(i, zerosetValArray);
                    }
                    else
                    {
                        updateScottPlot(i, CtlineVal);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            
        }

        private void button12_Click(object sender, EventArgs e)
        {
            baseCalculationAverage();//baseCalculation();
            setCtValueToDGV(dgv_interpretation_ct, Plotter.CtCycles);//setBaseValueToDataGridView(dgv_interpretation_ct, CtlineVal);
            setCtResultToDGV(dgv_interpretation_ct);
        }

        private void formsPlot3_Load(object sender, EventArgs e)
        {

        }

        private void formsPlot4_Load(object sender, EventArgs e)
        {

        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox6.Checked)
            {
                lb_opticMeasurementCycleSelection.Visible = true;
                dgv_opticMeasurementCycleSelection.Visible = true;
            }
            else if (!checkBox6.Checked)
            {
                lb_opticMeasurementCycleSelection.Visible = false;
                dgv_opticMeasurementCycleSelection.Visible = false;
            }
        }

        private void btnBaseCycleSelection_Save_Click(object sender, EventArgs e)
        {
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");

            string fileName = di.ToString() + "\\baseCalculationCycles.info";
            Save_Csv_BaseCycles(fileName, dgv_CycleForBaseCalculation, false);

            FindCyclesForBaseCalculation();//setBaseValue();
        }

        private void dgv_CycleForBaseCalculation_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            FindCyclesForBaseCalculation();
        }
                 
        private void button13_Click(object sender, EventArgs e)
        {
            loadCsv_BaseCycles(dgv_CycleForBaseCalculation);
        }

        private void button13_Click_1(object sender, EventArgs e)
        {
            DataGridView[] dgvArr = new DataGridView[4] { dgv_opticDatum_tube1, dgv_opticDatum_tube2, dgv_opticDatum_tube3, dgv_opticDatum_tube4 };
            MatchAndFindOpticDataForResult();
            FindCyclesForBaseCalculation();
            baseCalculationAverage();//baseCalculation();
            setCtValueToDGV(dgv_interpretation_ct, Plotter.CtCycles); //setBaseValueToDataGridView(dgv_interpretation_ct, CtlineVal);
            setCtResultToDGV(dgv_interpretation_ct);
            updateDataGridOpticDatas(dgvArr, DISPLAY_DATA);
            Plotter.ResetAllPlots(formsPlot1, formsPlot2, formsPlot3, formsPlot4);

            for (int i = 0; i < Plotter.CH_CNT; i++)
            {
                if (chkBox_baselineNoScale.Checked || chkBox_BaselineScale.Checked)
                {
                    for (int j = 0; j < Plotter.CH_CNT * Plotter.DYE_CNT; j++)
                    {
                        zerosetValArray[j] = (CtlineVal[j] - BaselineVal[j]) * scaleFactor[j]; //zerosetValArray[j] = CtlineVal[j] - BaselineVal[j];
                    }
                    updateScottPlot(i, zerosetValArray);
                }
                else
                {
                    updateScottPlot(i, CtlineVal);
                }
            }
        }

        private void btnBaseCycleSelection_Load_Click(object sender, EventArgs e)
        {
            loadCsv_BaseCycles(dgv_CycleForBaseCalculation);
        }

        private void loadCsv_BaseCycles(DataGridView dgv)
        {
            dgv.Rows.Clear();

            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists) di.Create();

            string fileName = di.ToString() + "\\baseCalculationCycles.info";

            string[] lines = File.ReadAllLines(fileName);

            int readNum = 1;
            string temp = "";
            for (int i = 1; i < lines.Length; i++) //데이터가 존재하는 라인일 때에만, label에 출력한다.
            {
                temp = lines[i];

                char[] sep = { ',' };

                string[] result = temp.Split(sep);
                //string[] data6 = new string[4] { temp, temp, temp, temp };
                int index = 0;
                //foreach (var item in result)
                //{
                //    data6[index++] = item;
                //}

                dgv.Rows.Add(result);
            }

            for (int i = 0; i < dataGridView_Manage.Rows.Count; i++)
            {
                if (i % 2 != 0)
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.White;
                }
                else
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                }
            }
        }

        private void cb_Recipe_Eng_SelectedIndexChanged(object sender, EventArgs e)
        {
            sm.testName = cb_Recipe_Eng.Text;
            cb_Test_Interpretation.Text = sm.testName;
            tb_Test_Interpretation.Text = sm.testName;
            updateTestInfo();

            //init_Test_Result();
            init_Analytic_Result();
            init_Interpretation_howTo();
        }

        private void button14_Click(object sender, EventArgs e)
        {
            CtCyclesCalculation();
        
            drawDataGridView(dgvCtTable1, Plotter.CtCycles[0], Plotter.CtCycles[1], Plotter.CtCycles[2], Plotter.CtCycles[3]);
            drawDataGridView(dgvCtTable2, Plotter.CtCycles[4], Plotter.CtCycles[5], Plotter.CtCycles[6], Plotter.CtCycles[7]);
            drawDataGridView(dgvCtTable3, Plotter.CtCycles[8], Plotter.CtCycles[9], Plotter.CtCycles[10], Plotter.CtCycles[11]);
            drawDataGridView(dgvCtTable4, Plotter.CtCycles[12], Plotter.CtCycles[13], Plotter.CtCycles[14], Plotter.CtCycles[15]);
        }

        private void dgvCtTable3_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
        }

        private void button15_Click(object sender, EventArgs e)
        {
            baseCalculationAverage();
            BaseStandardDeviation = baseCalculationDeviation();
            
        }

        private void button16_Click(object sender, EventArgs e)
        {
            FindCyclesForBaseCalculation();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button15_Click_1(object sender, EventArgs e)
        {
            DataGridView[] dgvArr = new DataGridView[4] { dgv_opticDatum_tube1, dgv_opticDatum_tube2, dgv_opticDatum_tube3, dgv_opticDatum_tube4 };
            
            //updateDataGridOpticDatum(dgvArr, DISPLAY_DATA);
            updateDataGridOpticDatas(dgvArr, MEASURED_DATA);
        }

        private void button17_Click(object sender, EventArgs e)
        {
            baseCalculationAverage();
            //baseCalculation_Avg_WithOutMinAndMax();// 
            BaseStandardDeviation = baseCalculationDeviation_WithoutMinMax();//baseCalculationDeviation();
        }

        private void button18_Click(object sender, EventArgs e)
        {

        }

        private void button19_Click(object sender, EventArgs e)
        {
            string dt = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
            // 결과 csv 저장
            string filePath2 = Application.StartupPath + @"\log";

            filePath2 += "/PCR " + dt + ".csv";

            Save_GridCsv(filePath2);
        }

        private void button20_Click(object sender, EventArgs e)
        {
            scaleFactorCalculation();
        }

        public void scaleFactorCalculation()
        {
            for (int i = 0; i < 16; i++)
            {
                scaleFactor[i] = (double)(2500 / (2500 - BaselineVal[i]));
            }
        }

        private void formsPlot2_Load(object sender, EventArgs e)
        {

        }

        private void button21_Click(object sender, EventArgs e)
        {
            double tempCt = 0;
            for (int j = 0; j < Plotter.COL_CNT; j++)
            {
                double value = (Convert.ToDouble(MEASURED_DATA[5, j]) - BaselineVal[5]) * scaleFactor[5];

                if (value < 0) value = 0;
                double line = (CtlineVal[5] - BaselineVal[5]) * scaleFactor[5];
                //if (Convert.ToDouble(MEASURED_DATA[i, j]) > CtlineVal[i])
                if (value > line)
                {
                    Plotter.CtCycles[5] = j; // start at 3 cycles
                    tempCt = j;
                    break;
                }
                else if (value < (CtlineVal[5] - BaselineVal[5]) * scaleFactor[5])
                {
                    Plotter.CtCycles[5] = 0;
                }
            }


            if (tempCt == 0)
            {
                //break;
            }
            else
            {
                double tempCtline = Convert.ToDouble(MEASURED_DATA[5, (int)tempCt - 1]);
                double delta = Math.Abs(((Convert.ToDouble(MEASURED_DATA[5, (int)tempCt]) - tempCtline)) / 100);
                delta *= scaleFactor[5];
                Plotter.CtCycles[5] = tempCt;
                for (int k = 0; k < 100; k++)
                {
                    Plotter.CtCycles[5] += 0.01;
                    if (((tempCtline - BaselineVal[5]) * scaleFactor[5] + (delta * k)) >= (CtlineVal[5] - BaselineVal[5]) * scaleFactor[5])
                    {
                        Plotter.CtCycles[5] += scaleFactor[5];
                        break;
                    }
                    //else
                    //{
                    //    if (k == 99) Plotter.CtCycles[i] = 0;
                    //}
                }

                if (Plotter.CtCycles[5] < 12) Plotter.CtCycles[5] = 0;
            }
            

        }

        private void groupBox5_Enter(object sender, EventArgs e)
        {

        }

        public void updateCycleCount()
        {
            lbl_CycleCount.Text = routine_cnt.ToString();
            lbl_CycleCount.ForeColor = Color.Green;
        }

        public void updateDeviceStatus(string status)
        {
            sm.DeviceStatus = status;
            lbl_Status.Text = sm.DeviceStatus;
            if (lbl_Status.Text == "Running") lbl_Status.ForeColor = Color.LawnGreen;
            else if (lbl_Status.Text == "Getting Ready") lbl_Status.ForeColor = Color.Blue;
            else if (lbl_Status.Text == "Stop") lbl_Status.ForeColor = Color.Red;

        }

        private void button22_Click(object sender, EventArgs e)
        {
          
        }

        public void sigmaScaleSave()
        {
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists)
            {
                di.Create();
            }
            string fileName = di.ToString() + "\\SigmaScale.info";
            
            // 기존 파일 삭제
            FileInfo fileDel = new FileInfo(fileName);
            if (fileDel.Exists) fileDel.Delete(); // 없어도 에러안남

            // 새로 저장
            StreamWriter sw = new StreamWriter(fileName, true);

            //first line
            string buff = dgv_interpretation_ct.Rows[0].Cells[1].FormattedValue.ToString();
            for (int i = 1; i < 16; i++)
            {
                buff += "," + dgv_interpretation_ct.Rows[i].Cells[1].FormattedValue.ToString();
            }
            // + tb_ID_IDManage.Text + "," + tb_PW_IDManage.Text + "," + tb_Right_IDManage.SelectedItem;
            
            
            sw.WriteLine(buff);

            sw.Close();
        }

        private void btn_SigmaScale_Save_Click(object sender, EventArgs e)
        {
            sigmaScaleSave();
        }

        public string[] SigmaScaleLoad()
        {
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists) di.Create();

            string fileName = di.ToString() + "\\SigmaScale.info";

            string[] lines = File.ReadAllLines(fileName);
            string[] result = new string[16];

            int readNum = 1;
            string temp = "";
            for (int i = 0; i < lines.Length; i++) //데이터가 존재하는 라인일 때에만, label에 출력한다.
            {
                temp = lines[i];

                char[] sep = { ',' };

                result = temp.Split(sep);
            }

            for (int i = 0; i < 16; i++)
            {
                dgv_interpretation_ct.Rows[i].Cells[1].Value = result[i];
            }
            return result;
        }

        private void btn_SigmaScale_Load_Click(object sender, EventArgs e)
        {
            SigmaScaleLoad();
        }

   

        private void tp_Interpretation_Enter(object sender, EventArgs e)
        {
            SigmaScaleLoad();
            tb_Test_Interpretation.Text = sm.testName;
                       
          
            init_Interpretation_howTo();

            for (int i = 0; i < 16; i++)
            {
                dgv_interpretation_ct.Rows[i].Cells[4].Value = sm.ct_Limit[i];
            }


        }

        private void progressBar_Tester_Click_1(object sender, EventArgs e)
        {

        }


        public void init_Test_Result()
        {
            if (sm.testName == "COVID")
            {
                dgv_testResult.ColumnCount = 4;
                dgv_testResult.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                dgv_testResult.RowHeadersVisible = false;

                dgv_testResult.Columns[0].Name = "";
                dgv_testResult.Columns[0].MinimumWidth = 320;
                dgv_testResult.Columns[1].Name = "COVID (POS or NEG)";
                dgv_testResult.Columns[1].MinimumWidth = 320;
                dgv_testResult.Columns[2].Name = "Presumptive POS";
                dgv_testResult.Columns[2].MinimumWidth = 320;
                dgv_testResult.Columns[3].Name = "Invalid Test";
                dgv_testResult.Columns[3].MinimumWidth = 320;



                dgv_testResult.SelectionMode = DataGridViewSelectionMode.CellSelect;
                dgv_testResult.MultiSelect = false;

                int rowCnt = dgv_testResult.Rows.GetRowCount(DataGridViewElementStates.Visible);

                for (int i = 1; i < rowCnt; i++)
                {
                    //if (dgvArr[j].Rows.GetRowCount(DataGridViewElementStates.Visible) != 0)
                    //{
                    dgv_testResult.Rows.RemoveAt(0);
                    //}
                }


                string[] tube1 = { "tube1", "", "" };
                string[] tube2 = { "tube2", "", "" };
                string[] tube3 = { "tube3", "", "" };
                string[] tube4 = { "tube4", "", "" };

                dgv_testResult.Rows.Add(tube1);
                dgv_testResult.Rows.Add(tube2);
                dgv_testResult.Rows.Add(tube3);
                dgv_testResult.Rows.Add(tube4);

            }
            else if (sm.testName == "TB")
            {
                dgv_testResult.ColumnCount = 3;
                dgv_testResult.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                dgv_testResult.RowHeadersVisible = false;

                dgv_testResult.Columns[0].Name = "MTC";
                dgv_testResult.Columns[0].MinimumWidth = 350;
                dgv_testResult.Columns[1].Name = "NTM";
                dgv_testResult.Columns[1].MinimumWidth = 350;
                dgv_testResult.Columns[2].Name = "RIF";
                dgv_testResult.Columns[2].MinimumWidth = 340;

                dgv_testResult.SelectionMode = DataGridViewSelectionMode.CellSelect;
                dgv_testResult.MultiSelect = false;

            }

        }

        public void init_Analytic_Result()
        {
            if (sm.testName == "COVID")
            {
                dgv_analyticResult.ColumnCount = 7;
                dgv_analyticResult.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                dgv_analyticResult.RowHeadersVisible = false;

                dgv_analyticResult.Columns[0].Name = "";
                dgv_analyticResult.Columns[0].MinimumWidth = 148;

                dgv_analyticResult.Columns[1].Name = "E1";
                dgv_analyticResult.Columns[1].MinimumWidth = 148;
                dgv_analyticResult.Columns[2].Name = "E1";
                dgv_analyticResult.Columns[2].MinimumWidth = 148;
                dgv_analyticResult.Columns[3].Name = "IC";
                dgv_analyticResult.Columns[3].MinimumWidth = 149;

                dgv_analyticResult.Columns[4].Name = "E1";
                dgv_analyticResult.Columns[4].MinimumWidth = 149;
                dgv_analyticResult.Columns[5].Name = "N1";
                dgv_analyticResult.Columns[5].MinimumWidth = 149;
                dgv_analyticResult.Columns[6].Name = "IC";
                dgv_analyticResult.Columns[6].MinimumWidth = 149;

                dgv_analyticResult.SelectionMode = DataGridViewSelectionMode.CellSelect;
                dgv_analyticResult.MultiSelect = false;

                int rowCnt = dgv_analyticResult.Rows.GetRowCount(DataGridViewElementStates.Visible);

                for (int i = 1; i < rowCnt; i++)
                {
                    //if (dgvArr[j].Rows.GetRowCount(DataGridViewElementStates.Visible) != 0)
                    //{
                    dgv_analyticResult.Rows.RemoveAt(0);
                    //}
                }

                string[] Ct = { "Ct", "", "", "", "", "", "" };
                string[] result = { "Result", "", "", "", "", "", "" };
                dgv_analyticResult.Rows.Add(Ct);
                dgv_analyticResult.Rows.Add(result);

            }
            else if (sm.testName == "TB")
            {
                dgv_analyticResult.ColumnCount = 9;
                dgv_analyticResult.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                dgv_analyticResult.RowHeadersVisible = false;

                dgv_analyticResult.Columns[0].Name = "";
                dgv_analyticResult.Columns[0].MinimumWidth = 115;

                dgv_analyticResult.Columns[1].Name = "MTC";
                dgv_analyticResult.Columns[1].MinimumWidth = 115;
                dgv_analyticResult.Columns[2].Name = "IC";
                dgv_analyticResult.Columns[2].MinimumWidth = 115;
                dgv_analyticResult.Columns[3].Name = "NTM";
                dgv_analyticResult.Columns[3].MinimumWidth = 115;
                dgv_analyticResult.Columns[4].Name = "RIF1";
                dgv_analyticResult.Columns[4].MinimumWidth = 115;
                dgv_analyticResult.Columns[5].Name = "RIF2";
                dgv_analyticResult.Columns[5].MinimumWidth = 115;
                dgv_analyticResult.Columns[6].Name = "RIF3";
                dgv_analyticResult.Columns[6].MinimumWidth = 115;
                dgv_analyticResult.Columns[7].Name = "RIF4";
                dgv_analyticResult.Columns[7].MinimumWidth = 115;
                dgv_analyticResult.Columns[8].Name = "RIF5";
                dgv_analyticResult.Columns[8].MinimumWidth = 115;


                dgv_analyticResult.SelectionMode = DataGridViewSelectionMode.CellSelect;
                dgv_analyticResult.MultiSelect = false;

                int rowCnt = dgv_analyticResult.Rows.GetRowCount(DataGridViewElementStates.Visible);

                for (int i = 1; i < rowCnt; i++)
                {
                    //if (dgvArr[j].Rows.GetRowCount(DataGridViewElementStates.Visible) != 0)
                    //{
                    dgv_analyticResult.Rows.RemoveAt(0);
                    //}
                }

                string[] Ct = { "Ct", "", "", "", "", "", "" };
                string[] result = { "Result", "", "", "", "", "", "" };

                dgv_analyticResult.Rows.Add(Ct);
                dgv_analyticResult.Rows.Add(result);

            }
        }



        public void init_Interpretation_howTo()
        {
            if (sm.testName == "COVID")
            {
                dgv_interpretation_howTo.ColumnCount = 7;
                dgv_interpretation_howTo.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                dgv_interpretation_howTo.RowHeadersVisible = false;

                dgv_interpretation_howTo.Columns[0].Name = "";
                dgv_interpretation_howTo.Columns[0].Width = 148;

                dgv_interpretation_howTo.Columns[1].Name = "E1";
                dgv_interpretation_howTo.Columns[1].Width = 148;
                dgv_interpretation_howTo.Columns[2].Name = "N1";
                dgv_interpretation_howTo.Columns[2].Width = 148;
                dgv_interpretation_howTo.Columns[3].Name = "IC1";
                dgv_interpretation_howTo.Columns[3].Width = 149;

                dgv_interpretation_howTo.Columns[4].Name = "E2";
                dgv_interpretation_howTo.Columns[4].Width = 149;
                dgv_interpretation_howTo.Columns[5].Name = "N2";
                dgv_interpretation_howTo.Columns[5].Width = 149;
                dgv_interpretation_howTo.Columns[6].Name = "IC2";
                dgv_interpretation_howTo.Columns[6].Width = 149;

                dgv_interpretation_howTo.SelectionMode = DataGridViewSelectionMode.CellSelect;
                dgv_interpretation_howTo.MultiSelect = false;

                string[] covid = { "COVID", "+", "+", "+", "", "", "" };
                string[] neg = { "NEG", "-", "-", "+", "", "", "" };
                string[] again = { "RETEST", "-", "+", "-" };
                
                dgv_interpretation_howTo.Rows.Add(covid);
                dgv_interpretation_howTo.Rows.Add(neg);
                dgv_interpretation_howTo.Rows.Add(again);


                int rowCnt = dgv_interpretation_howTo.Rows.GetRowCount(DataGridViewElementStates.Visible);

                for (int i = 1; i < rowCnt; i++)
                {
                    //if (dgvArr[j].Rows.GetRowCount(DataGridViewElementStates.Visible) != 0)
                    //{
                    dgv_interpretation_howTo.Rows.RemoveAt(0);
                    //}
                }
            }
            else if (sm.testName == "TB")
            {
                dgv_interpretation_howTo.ColumnCount = 9;
                dgv_interpretation_howTo.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                dgv_interpretation_howTo.RowHeadersVisible = false;

                dgv_interpretation_howTo.Columns[0].Name = "";
                dgv_interpretation_howTo.Columns[0].MinimumWidth = 120;
                dgv_interpretation_howTo.Columns[0].Width = 120;

                dgv_interpretation_howTo.Columns[1].Name = "FAM1";
                dgv_interpretation_howTo.Columns[1].MinimumWidth = 120;
                dgv_interpretation_howTo.Columns[1].Width = 120;

                dgv_interpretation_howTo.Columns[2].Name = "CY51";
                dgv_interpretation_howTo.Columns[2].MinimumWidth = 120;
                dgv_interpretation_howTo.Columns[2].Width = 120;

                dgv_interpretation_howTo.Columns[3].Name = "FAM2";
                dgv_interpretation_howTo.Columns[3].MinimumWidth = 120;
                dgv_interpretation_howTo.Columns[3].Width = 120;
                
                dgv_interpretation_howTo.Columns[4].Name = "CY52";
                dgv_interpretation_howTo.Columns[4].MinimumWidth = 120;
                dgv_interpretation_howTo.Columns[4].Width = 120;

                dgv_interpretation_howTo.Columns[5].Name = "FAM3";
                dgv_interpretation_howTo.Columns[5].MinimumWidth = 120;
                dgv_interpretation_howTo.Columns[5].Width = 120;

                dgv_interpretation_howTo.Columns[6].Name = "CY53";
                dgv_interpretation_howTo.Columns[6].MinimumWidth = 120;
                dgv_interpretation_howTo.Columns[6].Width = 120;

                dgv_interpretation_howTo.Columns[7].Name = "FAM4";
                dgv_interpretation_howTo.Columns[7].MinimumWidth = 120;
                dgv_interpretation_howTo.Columns[7].Width = 120;

                dgv_interpretation_howTo.Columns[8].Name = "CY54";
                dgv_interpretation_howTo.Columns[8].MinimumWidth = 120;
                dgv_interpretation_howTo.Columns[8].Width = 120;
                
                dgv_interpretation_howTo.SelectionMode = DataGridViewSelectionMode.CellSelect;
                dgv_interpretation_howTo.MultiSelect = false;
                
                string[] mtc = { "MTC", "+", "+", "", "", "", "", "", "" };
                string[] ntm = { "NTM", "-", "+", "+", "", "", "", "", "" };
                string[] neg = { "NEG", "-", "+", "-", "", "", "", "", "" };
                string[] rif1 = { "RIF1", "", "", "", "", "", "", "", "" };
                string[] rif2 = { "RIF2", "", "", "", "", "", "", "", "" };
                string[] rif3 = { "RIF3", "", "", "", "", "", "", "", "" };
                string[] rif4 = { "RIF4", "", "", "", "", "", "", "", "" };
                string[] rif5 = { "RIF5", "", "", "", "", "", "", "", "" };

                int rowCnt = dgv_interpretation_howTo.Rows.GetRowCount(DataGridViewElementStates.Visible);

                for (int i = 1; i < rowCnt; i++)
                {
                    //if (dgvArr[j].Rows.GetRowCount(DataGridViewElementStates.Visible) != 0)
                    //{
                    dgv_interpretation_howTo.Rows.RemoveAt(0);
                    //}
                }

                dgv_interpretation_howTo.Rows.Add(mtc);
                dgv_interpretation_howTo.Rows.Add(ntm);
                dgv_interpretation_howTo.Rows.Add(neg);
                dgv_interpretation_howTo.Rows.Add(rif1);
                dgv_interpretation_howTo.Rows.Add(rif2);
                dgv_interpretation_howTo.Rows.Add(rif3);
                dgv_interpretation_howTo.Rows.Add(rif4);
                dgv_interpretation_howTo.Rows.Add(rif5);
            }
        }


        public void update_Test_Result()
        {

        }

        public void autoUpdate_analytic_result()
        {
            //find same dye
            int idx = 0;
            for(int i = 0; i < 16; i++)
            {
                if( dgv_interpretation_howTo.Columns[0].HeaderText == dgv_interpretation_ct.Rows[i].Cells[2].FormattedValue.ToString())
                {
                    idx = i;
                }
            }
        }

        public void update_analytic_result()
        {
            init_Test_Result();

            if(sm.testName == "TB")
            {
                //MTC
                if (dgv_interpretation_howTo.Rows[0].Cells[1].Value == dgv_interpretation_ct.Rows[0].Cells[2].Value
                    && dgv_interpretation_howTo.Rows[0].Cells[2].Value == dgv_interpretation_ct.Rows[3].Cells[2].Value)
                {
                    dgv_testResult.Rows[0].Cells[0].Value = "+";
                }
                else
                {
                    dgv_testResult.Rows[0].Cells[0].Value = "-";
                }

                //NTM
                if (dgv_interpretation_howTo.Rows[0].Cells[1].Value == dgv_interpretation_ct.Rows[0].Cells[2].Value
                    && dgv_interpretation_howTo.Rows[0].Cells[2].Value == dgv_interpretation_ct.Rows[3].Cells[2].Value
                    && dgv_interpretation_howTo.Rows[1].Cells[1].Value == dgv_interpretation_ct.Rows[4].Cells[2].Value)
                {
                    dgv_testResult.Rows[0].Cells[1].Value = "+";
                }
                else
                {
                    dgv_testResult.Rows[0].Cells[1].Value = "-";
                }

                // N/A
                if (dgv_interpretation_howTo.Rows[0].Cells[1].Value == dgv_interpretation_ct.Rows[0].Cells[2].Value
                   && dgv_interpretation_howTo.Rows[0].Cells[2].Value == dgv_interpretation_ct.Rows[3].Cells[2].Value
                   && dgv_interpretation_howTo.Rows[1].Cells[1].Value == dgv_interpretation_ct.Rows[4].Cells[2].Value)
                {
                    dgv_testResult.Rows[0].Cells[2].Value = "+";
                }
                else
                {
                    dgv_testResult.Rows[0].Cells[2].Value = "-";
                }
            }

            if(sm.testName == "COVID")
            {
                //tube1 - covid (pos or neg)
                if (dgv_interpretation_ct.Rows[0].Cells[2].Value == "+"//FAM
                    && dgv_interpretation_ct.Rows[2].Cells[2].Value == "+" //HEX 
                    && dgv_interpretation_ct.Rows[3].Cells[2].Value == "+")//CY5
                {
                    dgv_testResult.Rows[0].Cells[1].Value = "+";
                    //dgv_testResult.Rows[0].Cells[].Value = "-";
                }
                else if((dgv_interpretation_ct.Rows[0].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[2].Cells[2].Value == "+" && dgv_interpretation_ct.Rows[3].Cells[2].Value == "+")) 
                {
                    dgv_testResult.Rows[0].Cells[1].Value = "+";
                    //dgv_testResult.Rows[0].Cells[2].Value = "-";
                }




                //presumtive pos
                if ((dgv_interpretation_ct.Rows[0].Cells[2].Value == "+" && dgv_interpretation_ct.Rows[2].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[3].Cells[2].Value == "+"))
                {
                    dgv_testResult.Rows[0].Cells[2].Value = "+";
                    //dgv_testResult.Rows[0].Cells[2].Value = "-";
                }
                //NEG
                if ((dgv_interpretation_ct.Rows[0].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[2].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[3].Cells[2].Value == "+"))
                {
                    dgv_testResult.Rows[0].Cells[1].Value = "-";
                    //dgv_testResult.Rows[0].Cells[2].Value = "-";
                }
                //invalid
                if ((dgv_interpretation_ct.Rows[3].Cells[2].Value == "-")
                    || (dgv_interpretation_ct.Rows[0].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[2].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[3].Cells[2].Value == "-"))
                {
                    dgv_testResult.Rows[0].Cells[3].Value = "+";
                    //dgv_testResult.Rows[0].Cells[2].Value = "-";
                }




                //tube2
                if (dgv_interpretation_ct.Rows[4].Cells[2].Value == "+" && dgv_interpretation_ct.Rows[6].Cells[2].Value == "+" && dgv_interpretation_ct.Rows[7].Cells[2].Value == "+")
                {
                    dgv_testResult.Rows[1].Cells[1].Value = "+";
                    //dgv_testResult.Rows[0].Cells[].Value = "-";
                }
                else if ((dgv_interpretation_ct.Rows[4].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[6].Cells[2].Value == "+" && dgv_interpretation_ct.Rows[7].Cells[2].Value == "+"))
                {
                    dgv_testResult.Rows[1].Cells[1].Value = "+";
                    //dgv_testResult.Rows[0].Cells[2].Value = "-";
                }
                //presumtive pos
                 if ((dgv_interpretation_ct.Rows[4].Cells[2].Value == "+" && dgv_interpretation_ct.Rows[6].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[7].Cells[2].Value == "+"))
                {
                    dgv_testResult.Rows[1].Cells[2].Value = "+";
                    //dgv_testResult.Rows[0].Cells[2].Value = "-";
                }
                //NEG
                if ((dgv_interpretation_ct.Rows[4].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[6].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[7].Cells[2].Value == "+"))
                {
                    dgv_testResult.Rows[1].Cells[1].Value = "-";
                    //dgv_testResult.Rows[0].Cells[2].Value = "-";
                }
                
                //invalid
                if (dgv_interpretation_ct.Rows[7].Cells[2].Value == "-"||
                    (dgv_interpretation_ct.Rows[4].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[6].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[7].Cells[2].Value == "-"))
                {
                    dgv_testResult.Rows[1].Cells[3].Value = "+";
                    //dgv_testResult.Rows[0].Cells[2].Value = "-";
                }
                /*
                else if ((dgv_interpretation_ct.Rows[4].Cells[2].Value == "+" && dgv_interpretation_ct.Rows[6].Cells[2].Value == "+" && dgv_interpretation_ct.Rows[7].Cells[2].Value == "-"))
                {
                    dgv_testResult.Rows[1].Cells[3].Value = "+";
                    //dgv_testResult.Rows[0].Cells[2].Value = "-";
                }
                */


                //tube3
                if (dgv_interpretation_ct.Rows[8].Cells[2].Value == "+" && dgv_interpretation_ct.Rows[10].Cells[2].Value == "+" && dgv_interpretation_ct.Rows[11].Cells[2].Value == "+")
                {
                    dgv_testResult.Rows[2].Cells[1].Value = "+";
                    //dgv_testResult.Rows[0].Cells[].Value = "-";
                }
                else if ((dgv_interpretation_ct.Rows[8].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[10].Cells[2].Value == "+" && dgv_interpretation_ct.Rows[11].Cells[2].Value == "+"))
                {
                    dgv_testResult.Rows[2].Cells[1].Value = "+";
                    //dgv_testResult.Rows[0].Cells[2].Value = "-";
                }
                //presumtive pos
                if ((dgv_interpretation_ct.Rows[8].Cells[2].Value == "+" && dgv_interpretation_ct.Rows[10].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[11].Cells[2].Value == "+"))
                {
                    dgv_testResult.Rows[2].Cells[2].Value = "+";
                    //dgv_testResult.Rows[0].Cells[2].Value = "-";
                }
                //NEG
                if ((dgv_interpretation_ct.Rows[8].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[10].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[11].Cells[2].Value == "+"))
                {
                    dgv_testResult.Rows[2].Cells[1].Value = "-";
                    //dgv_testResult.Rows[0].Cells[2].Value = "-";
                }
                //invalid
                if (dgv_interpretation_ct.Rows[11].Cells[2].Value == "-" ||
                    (dgv_interpretation_ct.Rows[8].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[10].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[11].Cells[2].Value == "-"))
                {
                    dgv_testResult.Rows[2].Cells[3].Value = "+";
                    //dgv_testResult.Rows[0].Cells[2].Value = "-";
                }


                //tube4
                if (dgv_interpretation_ct.Rows[12].Cells[2].Value == "+" && dgv_interpretation_ct.Rows[14].Cells[2].Value == "+" && dgv_interpretation_ct.Rows[15].Cells[2].Value == "+")
                {
                    dgv_testResult.Rows[3].Cells[1].Value = "+";
                    sm.COVID_RESULT4[0] = "+";
                    //dgv_testResult.Rows[0].Cells[].Value = "-";
                }
                else if ( dgv_interpretation_ct.Rows[14].Cells[2].Value == "+")
                {
                    dgv_testResult.Rows[3].Cells[1].Value = "+";
                    sm.COVID_RESULT4[0] = "+";
                    //dgv_testResult.Rows[0].Cells[].Value = "-";
                }
                else if (dgv_interpretation_ct.Rows[12].Cells[2].Value == "+")
                {
                    dgv_testResult.Rows[3].Cells[1].Value = "+";
                    sm.COVID_RESULT4[0] = "+";
                    //dgv_testResult.Rows[0].Cells[].Value = "-";
                }
                else if ((dgv_interpretation_ct.Rows[12].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[14].Cells[2].Value == "+" && dgv_interpretation_ct.Rows[15].Cells[2].Value == "+"))
                {
                    dgv_testResult.Rows[3].Cells[1].Value = "+";
                    sm.COVID_RESULT4[0] = "+";
                    //dgv_testResult.Rows[0].Cells[2].Value = "-";
                }


                //presumtive pos
                if ((dgv_interpretation_ct.Rows[12].Cells[2].Value == "+" && dgv_interpretation_ct.Rows[14].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[15].Cells[2].Value == "+"))
                {
                    dgv_testResult.Rows[3].Cells[2].Value = "+";
                    sm.COVID_RESULT4[1] = "+";
                    //dgv_testResult.Rows[0].Cells[2].Value = "-";
                }
                //NEG
                if ((dgv_interpretation_ct.Rows[12].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[14].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[15].Cells[2].Value == "+"))
                {
                    sm.COVID_RESULT4[0] = "-";
                    dgv_testResult.Rows[3].Cells[1].Value = "-";
                    //dgv_testResult.Rows[0].Cells[2].Value = "-";
                }
                //invalid
                if (dgv_interpretation_ct.Rows[15].Cells[2].Value == "-" ||
                    (dgv_interpretation_ct.Rows[12].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[14].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[15].Cells[2].Value == "-"))
                {
                    sm.COVID_RESULT4[2] = "+";
                    dgv_testResult.Rows[3].Cells[3].Value = "+";
                    //dgv_testResult.Rows[0].Cells[2].Value = "-";
                }




                /*
                //RETEST
                if ((dgv_interpretation_ct.Rows[0].Cells[2].Value == "+" && dgv_interpretation_ct.Rows[2].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[3].Cells[2].Value == "+")
                    || (dgv_interpretation_ct.Rows[0].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[2].Cells[2].Value == "+" && dgv_interpretation_ct.Rows[3].Cells[2].Value == "+")
                    || (dgv_interpretation_ct.Rows[0].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[2].Cells[2].Value == "-" && dgv_interpretation_ct.Rows[3].Cells[2].Value == "-"))
                {
                    dgv_testResult.Rows[0].Cells[2].Value = "+";
                }
                else
                {
                    dgv_testResult.Rows[0].Cells[2].Value = "-";
                }
                */


            }

        }

        private void tp_TestReport_Enter(object sender, EventArgs e)
        {
            //init_Test_Result();
            init_Analytic_Result();
            //update_analytic_result();
        }

        private void tp_tester_Enter(object sender, EventArgs e)
        {
            //init_Test_Result();
            init_Analytic_Result();
            //update_analytic_result();
        }

        //현재 EXE 파일이 생성 되는 폴더 위치
        
        private void btn_save_interpretation_Click(object sender, EventArgs e)
        {
            

            //CSV version
            
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            string fileName = di.ToString() + "\\" + sm.testName + "_interpretation.csv";
            Save_Csv_Interpretation(fileName, dgv_interpretation_howTo, true);//Save_Csv(fileName, dataGridView_Manage, true);
            

        }

        private void Save_Csv_Interpretation(string fileName, DataGridView dgv, bool header)
        {
            // 현재 선택한 셀의 정보를 삭제 후
            //dataGridView_Manage.Rows.RemoveAt(this.dataGridView_Manage.SelectedRows[0].Index);
            //int sIndex = dgv.CurrentCell.RowIndex;
            //dgv.Rows.RemoveAt(sIndex);

            // 그리드뷰를 파일로 저장함
            string delimiter = ",";  // 구분자
            FileStream fs = new FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            StreamWriter csvExport = new StreamWriter(fs, System.Text.Encoding.UTF8);

            if (dgv.Rows.Count == 0) return;

            // 헤더정보 출력
            if (header)
            {
                for (int i = 0; i < dgv.Columns.Count; i++)
                {
                    csvExport.Write(dgv.Columns[i].HeaderText);
                    if (i != dgv.Columns.Count - 1)
                    {
                        csvExport.Write(delimiter);
                    }
                }
            }

            csvExport.Write(csvExport.NewLine); // add new line

            // 데이터 출력
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (!row.IsNewRow)
                {
                    for (int i = 0; i < dgv.Columns.Count; i++)
                    {
                        csvExport.Write(row.Cells[i].Value);
                        if (i != dgv.Columns.Count - 1)
                        {
                            csvExport.Write(delimiter);
                        }
                    }
                    csvExport.Write(csvExport.NewLine); // write new line
                }
            }

            csvExport.Flush(); // flush from the buffers.
            csvExport.Close();
            fs.Close();
            MessageBox.Show("Interpretation Information Saved.", "Info Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btn_load_interpretation_Click(object sender, EventArgs e)
        {
            //load csv
            Interpretation_Load();
        }

        public void get_interpretation_info()
        {
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists) di.Create();

            string fileName = di.ToString() + "\\" + sm.testName + "_interpretation.csv";

            string[] lines = File.ReadAllLines(fileName);
            string[] result;

            int readNum = 1;
            string temp = "";

            //column header 
            temp = lines[0];
            char[] sep = { ',' };
            result = temp.Split(sep);

            for (int j = 0; j < result.Length; j++)
            {
                sm.dyeArr[j] = result[j];
            }
                       

            for (int i = 1; i < lines.Length; i++) //데이터가 존재하는 라인일 때에만, label에 출력한다.
            {
                temp = lines[i];

                result = temp.Split(sep);

                for (int j = 0; j < result.Length; j++)
                {

                    if (j == 0)
                    {
                        sm.diagnosisArr[i - 1] = result[j];
                    }
                    else
                    {
                        sm.howToInterpretationArr[i - 1, j - 1] = result[j];
                    }
                }
                
            }

            // get result each dye 

            System.IO.DirectoryInfo di2 = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di2.Exists) di2.Create();

            string fileName2 = di2.ToString() + "\\Result.csv";

            string[] lines2 = File.ReadAllLines(fileName2);
            string[] result2 = new string[16];

            int readNum2 = 1;
            string temp2 = "";
            for (int i = 0; i < lines2.Length; i++) //데이터가 존재하는 라인일 때에만, label에 출력한다.
            {
                temp2 = lines2[i];

                char[] sep2 = { ',' };

                result2 = temp2.Split(sep2);
                for (int j = 0; j < result2.Length; j++)
                {
                    sm.resultArr[j] = result2[j];
                }

            }
        }

        public void Interpretation_Load()
        {
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists) di.Create();

            string fileName = di.ToString() + "\\" + sm.testName + "_interpretation.csv";

            string[] lines = File.ReadAllLines(fileName);
            string[] result;

            int readNum = 1;
            string temp = "";

            for (int i = 1; i < lines.Length; i++) //데이터가 존재하는 라인일 때에만, label에 출력한다.
            {
                temp = lines[i];
                char[] sep = { ',' };
                result = temp.Split(sep);
                                
                if (dgv_interpretation_howTo.Rows.GetRowCount(DataGridViewElementStates.Visible) > 1)
                {
                    dgv_interpretation_howTo.Rows.RemoveAt(0);
                }
                dgv_interpretation_howTo.Rows.Add(result);                
            }
            //return result;
        }

        private void dgv_interpretation_ct_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void btnResultSave_Click(object sender, EventArgs e)
        {
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists)
            {
                di.Create();
            }
            string fileName = di.ToString() + "\\Result.csv";

            // 기존 파일 삭제
            FileInfo fileDel = new FileInfo(fileName);
            if (fileDel.Exists) fileDel.Delete(); // 없어도 에러안남

            // 새로 저장
            StreamWriter sw = new StreamWriter(fileName, true);

            //first line
            string buff = dgv_interpretation_ct.Rows[0].Cells[2].FormattedValue.ToString();
            for (int i = 1; i < 16; i++)
            {
                buff += "," + dgv_interpretation_ct.Rows[i].Cells[2].FormattedValue.ToString();
            }
            // + tb_ID_IDManage.Text + "," + tb_PW_IDManage.Text + "," + tb_Right_IDManage.SelectedItem;
            
            sw.WriteLine(buff);

            sw.Close();
        }

        private void ResultLoad_Click(object sender, EventArgs e)
        {
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists) di.Create();

            string fileName = di.ToString() + "\\Result.csv";

            string[] lines = File.ReadAllLines(fileName);
            string[] result = new string[16];

            int readNum = 1;
            string temp = "";
            for (int i = 0; i < lines.Length; i++) //데이터가 존재하는 라인일 때에만, label에 출력한다.
            {
                temp = lines[i];

                char[] sep = { ',' };

                result = temp.Split(sep);
            }

            for (int i = 0; i < 16; i++)
            {
                dgv_interpretation_ct.Rows[i].Cells[2].Value = result[i];
            }
        }

        private void button23_Click(object sender, EventArgs e)
        {
            get_interpretation_info();
       
        }

        private void tp_engineer_Click(object sender, EventArgs e)
        {

        }


      
       

        private void btn_Save_Json_Click(object sender, EventArgs e)
        {
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\data");
            if (!di.Exists)
            {
                di.Create();
            }
            string fileName = di.ToString() + "\\HowToDiagnosis.json";

            // 기존 파일 삭제
            FileInfo fileDel = new FileInfo(fileName);
            if (fileDel.Exists) fileDel.Delete(); // 없어도 에러안남

            //Read interpretation csv file----------------------------------------------------------------
            string fileName2 = di.ToString() + "\\" + sm.testName + "_interpretation.csv";

            string[] lines2 = File.ReadAllLines(fileName2);
            string[] result2;

            int readNum2 = 1;
            string temp2 = "";

            for (int i = 1; i < lines2.Length; i++) //데이터가 존재하는 라인일 때에만, label에 출력한다.
            {
                temp2 = lines2[i];
                //char[] sep = { ',' };
                //result2 = temp2.Split(sep);
            }
            //--------------------------------------------------------------

            JsonObjectCollection root = new JsonObjectCollection();
            JsonArrayCollection[] diagnosis;// = new JsonArrayCollection[16];

            temp2 = lines2[0];
            char[] sep = { ',' };
            result2 = temp2.Split(sep);

            for(int i = 0; i < result2.Length; i++)
            {
                //diagnosis[i] = new JsonArrayCollection(result2[i]);
            }

            //JsonArrayCollection diagnosis = new JsonArrayCollection("MTC");


            JsonObjectCollection sequence = new JsonObjectCollection();
            sequence.Add(new JsonStringValue("FAM1", "+"));
            sequence.Add(new JsonStringValue("HEX1", "+"));
            sequence.Add(new JsonStringValue("CY51", "+/-"));
            //diagnosis.Add(sequence);
            //root.Add(diagnosis);

            string strRoot = root.ToString();
            tb_HowToInterpretation.Text = strRoot;
            System.IO.File.WriteAllText(di.ToString() + "\\HowToDiagnosis.json", strRoot);
            //System.IO.File.WriteAllText(@"\Data"+"\\HowToDiagnosis.json", strRoot);
        }

        private void btn_load_Json_Click(object sender, EventArgs e)
        {
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\data");
            if (!di.Exists)
            {
                return; //di.Create();
            }
            string fileName = di.ToString() + "\\HowToDiagnosis.json";

            string strReturnValue = System.IO.File.ReadAllText(fileName);

            if (strReturnValue == "")
            {
                MessageBox.Show("load failure..."); return;
            }

            JsonTextParser jtr = new JsonTextParser();
            JsonObject jo = jtr.Parse(strReturnValue);
            JsonObjectCollection jac = (JsonObjectCollection)jo;
            JsonArrayCollection arr1 = jac["MTC"] as JsonArrayCollection;
            //JsonArrayCollection arr2 = jac["Test_Info"] as JsonArrayCollection;
            tb_HowToInterpretate.Text = "";

            foreach (JsonObjectCollection joc in arr1)
            {
                tb_HowToInterpretate.Text += "MTC" + System.Environment.NewLine;
                tb_HowToInterpretate.Text += "FAM1: " + joc["FAM1"].GetValue().ToString() + System.Environment.NewLine;
                tb_HowToInterpretate.Text += "HEX1: " + joc["HEX1"].GetValue().ToString() + System.Environment.NewLine;
                tb_HowToInterpretate.Text += "CY51: " + joc["CY51"].GetValue().ToString() + System.Environment.NewLine;
            }

        }

        private void btn_2TempMCU_Eng_Click(object sender, EventArgs e)
        {

        }

        private void tb_OCHoldSec_Eng_TextChanged(object sender, EventArgs e)
        {

        }

        private void btn_pdf_print_Click(object sender, EventArgs e)
        {
            printReport();
        }
        public void printReport()
        {
            //string filename = Application.StartupPath + @"\TestResult"; 

            try
            {
                string filename = sm.resultFileName.Replace("csv", "pdf");
                /*
                //printDocument1_PrintPage(null, null);
                string dt = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
                // 결과 pdf 인쇄
                string filePath = Application.StartupPath + @"\TestResult";

                filePath += "/PCR " + dt + ".pdf";
                */
                printDocument1.DocumentName = "PCR report printing...";
                printDocument1.PrinterSettings.PrinterName = "Microsoft Print to PDF";
                printDocument1.PrinterSettings.PrintFileName = filename;
                printDocument1.PrinterSettings.PrintToFile = true;
                printDocument1.Print(); // 인쇄 시작

              

                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                MessageBox.Show("프린트 실패했습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //MsgBox msgbox = new MsgBox("Info", "Please select a item", "", "OK", "");
                //msgbox.ShowDialog();
                //MessageBox.Show("Please reselect item.", "Info" , MessageBoxButtons.OK, MessageBoxIcon.Information );
            }

        }

        public void record_load()
        {
            string sourceDir = Application.StartupPath + @"\TestResult";

            string[] picList = Directory.GetFiles(sourceDir, "*.csv");
            string[,] resultArr = new string[65535, 12]; // 6만개 까지 저장됨 
            string[] result = new string[12];

            for (int j = 0; j < picList.Length; j++)
            {
                string[] lines = File.ReadAllLines(picList[j]);

                string temp = "";

                for (int i = 1; i < lines.Length; i++)
                {
                    temp = lines[i];
                    char[] separator = { ',' };
                    result = temp.Split(separator);
                }
                for (int k = 0; k < result.Length; k++)
                {
                    resultArr[j, k] = result[k];
                }

                if (dgv_test_records.Rows.GetRowCount(DataGridViewElementStates.Visible) > 1)
                {
                    dgv_test_records.Rows.RemoveAt(0);
                }

            }


            

            dgv_test_records.ColumnCount = 12;
            dgv_test_records.ColumnHeadersVisible = true;

            DataGridViewCellStyle columnHeaderStyle = new DataGridViewCellStyle();

            columnHeaderStyle.BackColor = Color.Beige;
            columnHeaderStyle.Font = new Font("Verdana", 10, FontStyle.Bold);
            dgv_test_records.ColumnHeadersDefaultCellStyle = columnHeaderStyle;

            

            //set the column header names.
            dgv_test_records.Columns[0].Name = "Date";
            dgv_test_records.Columns[0].MinimumWidth = 100;
            dgv_test_records.Columns[1].Name = "M/C No.";
            dgv_test_records.Columns[2].Name = "Test Name";

            dgv_test_records.Columns[3].Name = "Start Time";
            dgv_test_records.Columns[3].MinimumWidth = 200;

            dgv_test_records.Columns[4].Name = "End Time";
            dgv_test_records.Columns[4].MinimumWidth = 200;

            dgv_test_records.Columns[5].Name = "CartridgeID";
            dgv_test_records.Columns[5].MinimumWidth = 200;
            dgv_test_records.Columns[6].Name = "SampleID";
            dgv_test_records.Columns[6].MinimumWidth = 200;
            dgv_test_records.Columns[7].Name = "Result";
            dgv_test_records.Columns[7].MinimumWidth = 200;
            dgv_test_records.Columns[8].Name = "FileName";
            dgv_test_records.Columns[9].Name = "userName";
            dgv_test_records.Columns[10].Name = "userID";
            dgv_test_records.Columns[11].Name = "CT Values";

            string[] record_rows = new string[12];
            //dataGridView1.Rows[0].DefaultCellStyle.BackColor = Color.AliceBlue;
            for (int l = 0; l < picList.Length; l++)
            {
                for (int m = 0; m < 12; m++)
                {
                    record_rows[m] = resultArr[l, m];
                }
                dgv_test_records.Rows.Add(record_rows);
            }
            dgv_test_records.Sort(dgv_test_records.Columns[0], ListSortDirection.Ascending);

            //dgv_test_records.Columns[0].SortMode = DataGridViewColumnSortMode.Automatic;


        }
        public void update_testResult_final()
        {
            string[] row = { "", "", sm.testName, sm.StartTime, sm.EndTime, sm.CartridgeID, sm.SampleID, "", "", "", "" };
            dgv_testResult_final.Rows.Add(row);


            dgv_testResult_final.Rows[0].Cells[0].Value = DateTime.Now.ToShortDateString();
            dgv_testResult_final.Rows[0].Cells[1].Value = sm.machineNumber;
            dgv_testResult_final.Rows[0].Cells[2].Value = sm.testName;
            dgv_testResult_final.Rows[0].Cells[3].Value = sm.StartTime;
            dgv_testResult_final.Rows[0].Cells[4].Value = sm.EndTime;
            dgv_testResult_final.Rows[0].Cells[5].Value = sm.CartridgeID;
            dgv_testResult_final.Rows[0].Cells[6].Value = sm.SampleID;

            if (sm.COVID_RESULT4[0] == "+") dgv_testResult_final.Rows[0].Cells[7].Value = "P";//sm.COVID_RESULT1[0];
            else if (sm.COVID_RESULT4[0] == "-") dgv_testResult_final.Rows[0].Cells[7].Value = "N";
            else if (sm.COVID_RESULT4[1] == "+") dgv_testResult_final.Rows[0].Cells[7].Value = "Presumptive Positive";
            else
            {
                dgv_testResult_final.Rows[0].Cells[7].Value = "Invalid";
            }
            dgv_testResult_final.Rows[0].Cells[8].Value = sm.resultFileName;
            dgv_testResult_final.Rows[0].Cells[9].Value = sm.userName;
            dgv_testResult_final.Rows[0].Cells[10].Value = sm.userID;

            for(int i = 0;  i < 16; i++)
            {
                Plotter.CtCycles[i] = Math.Truncate(Plotter.CtCycles[i] * 100) / 100;
            }

            dgv_testResult_final.Rows[0].Cells[11].Value = Plotter.CtCycles[0].ToString() + " "
                + Plotter.CtCycles[1].ToString() + " "
                + Plotter.CtCycles[2].ToString() + " "
                + Plotter.CtCycles[3].ToString() + " "
                + Plotter.CtCycles[4].ToString() + " "
                + Plotter.CtCycles[5].ToString() + " "
                + Plotter.CtCycles[6].ToString() + " "
                + Plotter.CtCycles[7].ToString() + " "
                + Plotter.CtCycles[8].ToString() + " "
                + Plotter.CtCycles[9].ToString() + " "
                + Plotter.CtCycles[10].ToString() + " "
                + Plotter.CtCycles[11].ToString() + " "
                + Plotter.CtCycles[12].ToString() + " "
                + Plotter.CtCycles[13].ToString() + " "
                + Plotter.CtCycles[14].ToString() + " "
                + Plotter.CtCycles[15].ToString();

            dgv_testResult_final.Update();

            string dt = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
            string filePath2 = Application.StartupPath + @"\TestResult";
            filePath2 += "/TestResult " + dt + ".csv";

            dgv_testResult_final.Rows[0].Cells[8].Value = filePath2;
            Save_GridCsv_dgv(filePath2, dgv_testResult_final);
        }

        private void Save_GridCsv_dgv(string fileName, DataGridView dgvArr)
        {
            //DataGridView[] dgvArr = { dataGridView1_Engineer, dataGridView2_Engineer, dataGridView3_Engineer, dataGridView4_Engineer };
            //DataGridView[] dgvArr = { dgv_opticDatum_tube1, dgv_opticDatum_tube2, dgv_opticDatum_tube3, dgv_opticDatum_tube4 };



            // 그리드뷰를 파일로 저장함
            string delimiter = ",";  // 구분자
            FileStream fs = new FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            StreamWriter csvExport = new StreamWriter(fs, System.Text.Encoding.UTF8);

            if (dgvArr.Rows.Count == 0) return;

            // 헤더정보 출력
            if (true)
            {
                for (int i = 0; i < dgvArr.Columns.Count; i++)
                {
                    csvExport.Write(dgvArr.Columns[i].HeaderText);
                    if (i != dgvArr.Columns.Count - 1)
                    {
                        csvExport.Write(delimiter);
                    }
                }
            }
            csvExport.Write(csvExport.NewLine); // add new line

            // 데이터 출력
            foreach (DataGridViewRow row in dgvArr.Rows)
            {
                if (!row.IsNewRow)
                {
                    for (int i = 0; i < dgvArr.Columns.Count; i++)
                    {
                        csvExport.Write(row.Cells[i].Value);
                        if (i != dgvArr.Columns.Count - 1)
                        {
                            csvExport.Write(delimiter);
                        }
                    }
                    csvExport.Write(csvExport.NewLine); // write new line
                }
            }
            csvExport.Flush(); // flush from the buffers.
            csvExport.Close();
            fs.Close();
        }

        private void tabControl_Tester_SelectedIndexChanged(object sender, EventArgs e)
        {
            record_load();
        }

        private void btn_ct_Limit_save_Click(object sender, EventArgs e)
        {
            try
            {

                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
                if (!di.Exists)
                {
                    di.Create();
                }
                string fileName = di.ToString() + "\\ctLimit.info";

                // 기존 파일 삭제
                FileInfo fileDel = new FileInfo(fileName);
                if (fileDel.Exists) fileDel.Delete(); // 없어도 에러안남

                // 새로 저장
                StreamWriter sw = new StreamWriter(fileName, true);

                //first line
                string buff = dgv_interpretation_ct.Rows[0].Cells[4].FormattedValue.ToString();
                for (int i = 1; i < 16; i++)
                {
                    buff += "," + dgv_interpretation_ct.Rows[i].Cells[4].FormattedValue.ToString();
                }
                // + tb_ID_IDManage.Text + "," + tb_PW_IDManage.Text + "," + tb_Right_IDManage.SelectedItem;

                sw.WriteLine(buff);

                sw.Close();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }


        }

        private void btn_ct_limit_load_Click(object sender, EventArgs e)
        {
            try
            {
                ctLimit_Load();
                for (int i = 0; i < 16; i++)
                {
                    dgv_interpretation_ct.Rows[i].Cells[4].Value = sm.ct_Limit[i];
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }


        }

        public void ctLimit_Load()
        {
            try
            {
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
                if (!di.Exists) di.Create();

                string fileName = di.ToString() + "\\ctLimit.info";

                string[] lines = File.ReadAllLines(fileName);
                string[] result = new string[16];

                int readNum = 1;
                string temp = "";
                for (int i = 0; i < lines.Length; i++) //데이터가 존재하는 라인일 때에만, label에 출력한다.
                {
                    temp = lines[i];

                    char[] sep = { ',' };

                    result = temp.Split(sep);
                }

                for (int i = 0; i < 16; i++)
                {
                    sm.ct_Limit[i] = result[i];
                    //sm.ct_Limit[i] = result[i];

                    //dgv_interpretation_ct.Rows[i].Cells[4].Value = result[i];
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

           
        }

        private void tp_testRecords_Enter(object sender, EventArgs e)
        {
            //record_load();
        }

        private void dgv_test_records_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {

                int index = dgv_test_records.SelectedRows[0].Index;
                sm.resultFileName = dgv_test_records.Rows[index].Cells[8].FormattedValue.ToString();

                sm.RecordInfoArr = new string[27];
                sm.RecordInfoArr[0] = dgv_test_records.Rows[index].Cells[0].FormattedValue.ToString(); //Date
                sm.RecordInfoArr[1] = dgv_test_records.Rows[index].Cells[1].FormattedValue.ToString(); //Machine Number
                sm.RecordInfoArr[2] = dgv_test_records.Rows[index].Cells[2].FormattedValue.ToString(); // TestName
                sm.RecordInfoArr[3] = dgv_test_records.Rows[index].Cells[3].FormattedValue.ToString(); //StartTime
                sm.RecordInfoArr[4] = dgv_test_records.Rows[index].Cells[4].FormattedValue.ToString(); // EndTime
                sm.RecordInfoArr[5] = dgv_test_records.Rows[index].Cells[5].FormattedValue.ToString(); // Cartridge ID
                sm.RecordInfoArr[6] = dgv_test_records.Rows[index].Cells[6].FormattedValue.ToString(); // Sample ID
                sm.RecordInfoArr[7] = dgv_test_records.Rows[index].Cells[7].FormattedValue.ToString(); // Result
                sm.RecordInfoArr[8] = dgv_test_records.Rows[index].Cells[8].FormattedValue.ToString(); // fileName 

                sm.RecordInfoArr[9] = dgv_test_records.Rows[index].Cells[9].FormattedValue.ToString(); //userName
                sm.RecordInfoArr[10] = dgv_test_records.Rows[index].Cells[10].FormattedValue.ToString(); //userID

                string temp = dgv_test_records.Rows[index].Cells[11].FormattedValue.ToString(); //CT values
                char[] separator = { ' ' };
                string[] result = temp.Split(separator);

                for (int i = 0; i < 16; i++)
                {
                    sm.RecordInfoArr[11 + i] = result[i];
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                MessageBox.Show("검사결과를 선택하세요.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
 
            
        }

        private void dgv_testResult_final_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void btn_pdf_open_Click(object sender, EventArgs e)
        {
            /*
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "PDF Files|*.pdf";
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                if (fileDialog.FileName.Length > 0)
                {
                   
                    //axAcroPDF1.LoadFile(fileDialog.FileName);
                }
            }
            */
           
        }

        private void btn_pdf_close_Click(object sender, EventArgs e)
        {
            

        }

        private void tp_ReportViewer_Enter(object sender, EventArgs e)
        {
            try
            {
                string filename = sm.resultFileName.Replace("csv", "pdf");
                axAcroPDF1.LoadFile(filename);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                MessageBox.Show("PDF를 프린트 해주세요.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            
        }

        private void tp_ReportViewer_Leave(object sender, EventArgs e)
        {
            //axAcroPDF1.Dispose();
        }

        private void tb_MachineNum_Eng_TextChanged(object sender, EventArgs e)
        {

        }
    }
    #endregion

}