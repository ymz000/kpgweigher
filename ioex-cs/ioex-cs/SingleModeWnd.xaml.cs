﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.IO;

namespace ioex_cs
{
    /// <summary>
    /// Interaction logic for SingleMode.xaml
    /// </summary>
    public partial class SingleMode : Window
    {
        System.Windows.Forms.Timer uiTimer;

        private UIPacker curr_packer
        {
            get
            {
                App p = Application.Current as App;
                return p.packers[0];
            }
        }
        private Queue<string> lastcalls;
        
        private string lastcall
        {
            get
            {
                if (lastcalls.Count == 0)
                    return "";
                else
                    return lastcalls.Peek();
            }
            set
            {
                if (value == "")
                {
                    if (lastcalls.Count > 0)
                        lastcalls.Dequeue();
                }
                else
                {
                    if(!lastcalls.Contains(value))
                        lastcalls.Enqueue(value);
                }
            }
        }
        private bool bSelectAll = false;
        private int curr_node_index { get; set; }
        private void HandleLongCommand()
        {
            App p = Application.Current as App;
            p.agent.Stop(500);
            curr_packer.nc.Stop(500);
            if (lastcall == "StartStop")
            {
                ToggleStartStop();
                lastcall = "";
            }

            if (lastcall == "UpdateProdNo")
            {
                curr_packer.LoadPackConfig(curr_packer.curr_cfg.product_no,false);
                curr_packer.SaveCurrentConfig(1+2+4);
                UpdateUI();
                lastcall = "";
            }
            if (lastcall == "UpdateUI")
            {
                
                UpdateUI();
                lastcall = "";
            }
            if (lastcall == "ApplyToAll")
            {
                
                sub_applyall();
                lastcall = "";
            }
            if (lastcall == "select_prdno")
            {
                new_prd_Click(null, null);
                lastcall = "";
            }
            if (lastcall == "save_config")
            {
                curr_packer.SaveCurrentConfig(1 + 2 + 4);
                lastcall = "";
                //MessageBox.Show(StringResource.str("store_done"));
            }
            if (lastcall == "")
            {
                txt_oper.Visibility = Visibility.Hidden;
                bg_oper.Visibility = Visibility.Hidden;
                p.agent.Resume();
                curr_packer.nc.Resume();
            }
        }
        private bool _contentLoaded2 = false;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void MyInitializeComponent(int nodenumber)
        {
            if (_contentLoaded2)
            {
                return;
            }
            _contentLoaded2 = true;

            if (nodenumber <= 10)
            {
                System.Windows.Application.LoadComponent(this, new System.Uri("/ioex-cs;component/singlemodewnd.xaml", System.UriKind.Relative));
            }
            else
            {
                System.Windows.Application.LoadComponent(this, new System.Uri("/ioex-cs;component/Resources/singlemodewnd14.xaml", System.UriKind.Relative));
            }
        }

        public SingleMode(int nodenumber)
        {
            MyInitializeComponent(nodenumber);
            lastcalls = new Queue<string>();
            curr_btn = null;
            last_btn = null;
            curr_node_index = -1;
            uiTimer = new System.Windows.Forms.Timer();
            uiTimer.Tick += new EventHandler(uiTimer_Tick);
            uiTimer.Interval = 100;
            uiTimer.Start();
        }

        private App currentApp()
        {
            return (System.Windows.Application.Current as App);
        }

        private Button curr_btn;
        private Button last_btn;
        private object tlock = new object();
        private static bool tmlock = false;
        void uiTimer_Tick(object sender, EventArgs e)
        {
            if (!this.IsVisible)
                return;
            if (tmlock)
                return;
            tmlock = true;
            
            {
                App p = Application.Current as App;
                if (NodeCombination.q_hits.Count > 0)
                {
                    HitCombineNodeUI(NodeCombination.q_hits.Dequeue());
                    tmlock = false;
                    return;
                }
                if (p.agent.bNeedRefresh)
                {
                    p.agent.bNeedRefresh = false;
                    RefreshNodeUI();
                    if (curr_packer.status != PackerStatus.RUNNING)
                    {
                        currentApp().agent.ClearWeights();
                        currentApp().agent.bWeightReady = false;
                    }
                }
                if ((curr_node_index == -1))
                {
                    bucket_click(1);
                }
                else
                {
                    if (lastcall != "")
                        HandleLongCommand();
                }
            }
            tmlock = false;
        }
        private void RefreshNodeUI()
        {
            foreach (UIPacker pk in currentApp().packers)
            {
                foreach (byte naddr in pk.weight_nodes)
                {
                    string param = "wei_node" + naddr.ToString();
                    Label lb = this.FindName(param) as Label;
                    Button btn = this.FindName(param.Replace("wei_node", "bucket")) as Button;
                    byte n = (byte)(RunMode.StringToId(param));
                    NodeAgent agent = currentApp().packers[pk._pack_id].agent;

                    double wt = agent.weight(n);
                    if (wt > -1000 && wt <= WeighNode.MAX_VALID_WEIGHT)
                        lb.Content = agent.weight(n).ToString("F1");
                    else
                    {
                        if (wt > WeighNode.MAX_VALID_WEIGHT && wt < 65537)
                            lb.Content = "ERR";
                    }

                    if (agent.GetStatus(n) == NodeStatus.ST_LOST || agent.GetStatus(n) == NodeStatus.ST_DISABLED)
                    {
                        btn.Template = this.FindResource("WeightBarError") as ControlTemplate;
                    }
                    if (agent.GetStatus(n) == NodeStatus.ST_IDLE)
                    {
                        if (btn == curr_btn || bSelectAll)
                            btn.Template = this.FindResource("WeightBarFocus") as ControlTemplate;
                        else
                            btn.Template = this.FindResource("WeightBar") as ControlTemplate;
                    }
                    btn.ApplyTemplate();
                }
            }
            UIPacker p = curr_packer;
            if (p.status == PackerStatus.RUNNING)
            {
                lbl_speed.Content = p.speed.ToString();
                lbl_lastweight.Content = p.last_pack_weight.ToString("F1");
                lbl_totalpack.Content = p.total_sim_packs.ToString();
                lbl_totalweights.Content = p.total_sim_weights.ToString("F1");
            }
        }
        public void Disable(Visibility state)
        {
            btn_trial.Visibility = state;
        }
        private void HitCombineNodeUI(CombineEventArgs ce)
        {
            foreach (byte naddr in currentApp().packers[ce.packer_id].weight_nodes)
            {
                string param = "wei_node" + naddr.ToString();
                Label lb = this.FindName(param) as Label;
                Button btn = this.FindName(param.Replace("wei_node", "bucket")) as Button;
                byte n = (byte)(RunMode.StringToId(param));
                NodeAgent agent = currentApp().packers[ce.packer_id].agent;

                if (!ce.release_addrs.Contains(n))
                    continue;
                double wt = agent.weight(n);
                if (wt > -1000 && wt <= WeighNode.MAX_VALID_WEIGHT)
                    lb.Content = agent.weight(n).ToString("F1");

                if (agent.GetStatus(n) == NodeStatus.ST_LOST || agent.GetStatus(n) == NodeStatus.ST_DISABLED)
                {
                    btn.Template = this.FindResource("WeightBarError") as ControlTemplate;
                }else{
                    if (n != curr_packer.vib_addr)
                    {
                        if(ce.release_addrs.Contains(n))
                            btn.Template = this.FindResource("WeightBarRelease") as ControlTemplate;
                        else{
                            btn.Template = this.FindResource("WeightBar") as ControlTemplate;
                            if (agent.GetStatus(n) == NodeStatus.ST_IDLE)
                            {
                                if (btn == curr_btn || bSelectAll)
                                    btn.Template = this.FindResource("WeightBarFocus") as ControlTemplate;
                            }
                        }
                     }
                }

                btn.ApplyTemplate();
            }
        }
        private void weibucket_Click(object sender, RoutedEventArgs e)
        {
            bucket_click(ButtonToId(sender));
            bSelectAll = false;
        }
        private void passbucket_Click(object sender, RoutedEventArgs e)
        {
            bucket_click(ButtonToId(sender));
            bSelectAll = false;
        }
        private void vibbucket_Click(object sender, RoutedEventArgs e)
        {
            bucket_click(ButtonToId(sender));
            bSelectAll = false;
        }
        private int ButtonToId(object sender)
        {
            StringBuilder sb = new StringBuilder();
            Regex re = new Regex("(\\d+)");
            Match m2 = re.Match((sender as Control).Name);
            if (m2.Success)
                return int.Parse(m2.Groups[0].Value);
            else
                return -1;

        }
        
        private Button IdToButton(int nodeid)
        {
            return this.FindName("bucket" + nodeid) as Button;
        }
        private void bucket_click(int nodeid)
        {
            App p = Application.Current as App;
            int last_node = curr_node_index;
            if (nodeid < 1)
            {
                return;
            }
            
            last_btn = IdToButton(last_node);
            curr_btn = IdToButton(nodeid);

            curr_node_index = nodeid;

            if (last_btn is Button)
            {
                last_btn.Template = this.FindResource("WeightBar") as ControlTemplate;
                last_btn.ApplyTemplate();
            }
            if (curr_btn is Button)
            {
                curr_btn.Template = this.FindResource("WeightBarFocus") as ControlTemplate;
                curr_btn.ApplyTemplate();
            }
            ShowStatus("loading");
            //update parameter of the current node to UI.
            lastcall = "UpdateUI";
            return;
            
            
        }
        public void UpdateUI()
        {
            App p = Application.Current as App;
            NodeAgent n = curr_packer.agent;
            byte vn = curr_packer.vib_addr;
            if (curr_node_index < 0)
                return;
             
            sub_freq_input.Content = n.GetNodeReg((byte)curr_node_index, "magnet_freq");
            sub_amp_input.Content = n.GetNodeReg((byte)curr_node_index, "magnet_amp");
            sub_time_input.Content = n.GetNodeReg((byte)curr_node_index, "magnet_time");
            sub_filter_input.Content = n.GetNodeReg((byte)curr_node_index, "cs_gain_wordrate");
            wei_otime_input.Content = n.GetNodeReg((byte)curr_node_index, "open_w");
            wei_dtime_input.Content = n.GetNodeReg((byte)curr_node_index, "delay_w");
            col_dtime_input.Content = n.GetNodeReg((byte)curr_node_index, "delay_s");
            col_otime_input.Content = n.GetNodeReg((byte)curr_node_index, "open_s");
            openwei_input.Content = n.GetNodeReg((byte)curr_node_index, "delay_f");


            motor_speed_input.Content = n.GetNodeReg((byte)curr_node_index, "motor_speed");

            run_freq.Content = n.GetNodeReg(curr_packer.vib_addr,"magnet_freq");
            run_amp.Content = n.GetNodeReg(curr_packer.vib_addr,"magnet_amp");
            run_time.Content = n.GetNodeReg(curr_packer.vib_addr, "magnet_time");
            btn_prd_no.Content = curr_packer.curr_cfg.product_no.ToString();
            btn_prd_desc.Content = curr_packer.curr_cfg.product_desc.ToString();
            btn_target.Content = curr_packer.curr_cfg.target.ToString() +StringResource.str("gram");
            btn_uvar.Content = curr_packer.curr_cfg.upper_var.ToString() + StringResource.str("gram");
            btn_dvar.Content = curr_packer.curr_cfg.lower_var.ToString() + StringResource.str("gram");

            cb_autoamp.IsChecked = (curr_packer.curr_cfg.target_comb > 0);
            if (cb_autoamp.IsChecked.Value)
            {
                cb_autoamp.Content = StringResource.str("autoamp_on") + curr_packer.curr_cfg.target_comb.ToString();
            }
            else
            {
                cb_autoamp.Content = StringResource.str("autoamp_off");
            }

            lbl_currNode.Content = (curr_node_index).ToString();
            Rectangle rect = this.FindName("ellipseWithImageBrush") as Rectangle;
            //load the corresponding pictiure.
            if (File.Exists(ProdNum.baseDir + "\\prodpic\\" + StringResource.language + "\\" + curr_packer.curr_cfg.product_desc.ToString() + ".jpg"))
                (rect.Fill as ImageBrush).ImageSource = new BitmapImage(new Uri(ProdNum.baseDir + "\\prodpic\\" + StringResource.language + "\\" + curr_packer.curr_cfg.product_desc.ToString() + ".jpg"));
            else
                (rect.Fill as ImageBrush).ImageSource = new BitmapImage(new Uri(ProdNum.baseDir + "\\prodpic\\default.jpg"));
        }
        private void node_reg(string regname)
        {
            App p = Application.Current as App;
            if (curr_packer.status == PackerStatus.RUNNING)
            {
                return;
            }
            p.kbdwnd.Init(StringResource.str("enter_" + regname), regname, false, KbdData);
        }
        public void KbdData(string param, string data)
        {
            //update the display based on keyboard input
            App p = Application.Current as App;
            bool bNeedUpdateComb = false; //whether target weight of each node is required
            try
            {
                
                UIPacker pack = curr_packer;
                NodeAgent n = curr_packer.agent;
                if (param == "sub_freq_input")
                {
                    n.SetNodeReg((byte)curr_node_index, "magnet_freq", UInt32.Parse(data));
                    apply_regs.Insert(0, "magnet_freq");
                }
                if (param == "sub_amp_input")
                {
                    n.SetNodeReg((byte)curr_node_index, "magnet_amp", UInt32.Parse(data));
                    apply_regs.Insert(0, "magnet_amp");
                }
                if (param == "sub_time_input")
                {
                    n.SetNodeReg((byte)curr_node_index, "magnet_time", UInt32.Parse(data));
                    apply_regs.Insert(0, "magnet_time");
                }
                if (param == "sub_filter_input")
                {
                    n.SetNodeReg((byte)curr_node_index, "cs_gain_wordrate", UInt32.Parse(data));
                    apply_regs.Insert(0, "cs_gain_wordrate");
                }

                if (param == "wei_otime_input")
                {
                    n.SetNodeReg((byte)curr_node_index, "open_w", UInt32.Parse(data));
                    apply_regs.Insert(0, "open_w");
                }
                if (param == "wei_dtime_input")
                {
                    n.SetNodeReg((byte)curr_node_index, "delay_w", UInt32.Parse(data));
                    apply_regs.Insert(0, "delay_w");
                }
                if (param == "col_dtime_input")
                {
                    n.SetNodeReg((byte)curr_node_index, "delay_s", UInt32.Parse(data));
                    apply_regs.Insert(0, "delay_s");
                }
                if (param == "col_otime_input")
                {
                    n.SetNodeReg((byte)curr_node_index, "open_s", UInt32.Parse(data));
                    apply_regs.Insert(0, "open_s");
                }
                if (param == "openwei_input")
                {
                    n.SetNodeReg((byte)curr_node_index, "delay_f", UInt32.Parse(data));
                    apply_regs.Insert(0, "delay_f");
                }
                if (param == "motor_speed_input")
                {
                    n.SetNodeReg((byte)curr_node_index, "motor_speed", UInt32.Parse(data));
                    apply_regs.Insert(0, "motor_speed");
                }
                if (param == "run_freq")
                {
                    n.SetNodeReg(curr_packer.vib_addr, "magnet_freq", UInt32.Parse(data));
                }
                if (param == "run_time")
                {
                    n.SetNodeReg(curr_packer.vib_addr, "magnet_time", UInt32.Parse(data));
                }
                
                if (param == "run_amp")
                {
                    n.SetNodeReg(curr_packer.vib_addr, "magnet_amp", UInt32.Parse(data));
                }
                if (param == "autoamp")
                {
                    curr_packer.curr_cfg.target_comb  = Double.Parse(data);
                    cb_autoamp.IsChecked = (curr_packer.curr_cfg.target_comb > 1);
                    bNeedUpdateComb = true;
                }
                if (param == "target")
                {
                    curr_packer.curr_cfg.target = Double.Parse(data);
                    bNeedUpdateComb = true;
                }
                if (param == "uvar")
                {
                    curr_packer.curr_cfg.upper_var = Double.Parse(data);
                }
                if (param == "dvar")
                {
                    curr_packer.curr_cfg.lower_var = Double.Parse(data);
                }

           
                ShowStatus("modifying");
                if (bNeedUpdateComb)
                    curr_packer.UpdateEachNodeTarget();
                lastcall = "UpdateUI";
            }
            catch (System.Exception e)
            {
                //MessageBox.Show("Invalid Parameter");
                return;
            }
        }
        private void input_GotFocus(object sender, RoutedEventArgs e)
        {
            App p = Application.Current as App;
            if (curr_packer.status == PackerStatus.RUNNING)
            {
                //MessageBox.Show("is_running");
                return;
            }
            string regname = (sender as Label).Name;
            p.kbdwnd.Init(StringResource.str("enter_" + regname), regname, false, KbdData);
        }

        private void btn_action_Click(object sender, RoutedEventArgs e)
        {

            App p = Application.Current as App;
            byte n = (byte)curr_node_index;
            string name = (sender as Button).Name;
            if (name == "btn_mainvib")
            {
                curr_packer.VibAction("fill");
            }
            if (name == "btn_pack")
            {
                curr_packer.agent.TriggerPacker(curr_packer.vib_addr);
            }
            if (name == "btn_pass" || name == "btn_empty" || name == "btn_fill" || name == "btn_flash" || name == "btn_zero")
            {
                if (!bSelectAll)
                    curr_packer.WeightAction(n, name.Replace("btn_", ""));
                else
                {
                    if (name == "btn_flash")
                    {
                        foreach (byte addr in curr_packer.weight_nodes)
                        {
                            curr_packer.WeightAction(addr, name.Replace("btn_", ""));
                        }
                    }
                    else
                    {
                        curr_packer.GroupAction(name.Replace("btn_", ""));
                    }
                }
            }
            
        }
        private void prd_pic_selected(string item)
        {
            App p = Application.Current as App;
            curr_packer.curr_cfg.product_desc = item;
            ShowStatus("loading");
            lastcall = "UpdateUI";
        }

        private void prd_desc_selected(string item)
        {
            App p = Application.Current as App;
            curr_packer.curr_cfg.product_desc = item;
            ShowStatus("loading");
            lastcall = "UpdateUI";
        }
        private void prd_no_selected(string item)
        {
            App p = Application.Current as App;
            curr_packer.curr_cfg.product_no = item;

            ShowStatus("loading");
            lastcall = "UpdateProdNo";
        }
        private void btn_prd_desc_Click(object sender, RoutedEventArgs e)
        {
            App p = Application.Current as App;
            p.prodwnd.Init(prd_desc_selected);
        }
        private void ToggleStartStop()
        {
            
            App p = Application.Current as App;
            if (curr_packer.status == PackerStatus.RUNNING)
            {
                curr_packer.StopRun();
                this.btn_trial.Content = StringResource.str("sall_start");
            }
            else
            {
                curr_packer.bSimulate = true;
                curr_packer.StartRun();
                this.btn_trial.Content = StringResource.str("sall_stop");

            }

        }
        private void btn_trial_Click(object sender, RoutedEventArgs e)
        {
            lastcall = "StartStop";
            if(curr_packer.status == PackerStatus.RUNNING)
                ShowStatus(StringResource.str("stopping"));
            else
                ShowStatus(StringResource.str("starting"));
        }
        private void btn_save_Click(object sender, RoutedEventArgs e)
        {
            ShowStatus("store_setting");
            lastcall = "save_config";
        }

        private void btn_prd_no_Click(object sender, RoutedEventArgs e)
        {
            App p = Application.Current as App;
            p.prodnum.Init(prd_no_selected,true);
        }

        private void btn_pack_dvar_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current as App).kbdwnd.Init(StringResource.str("enter_dvar"), "dvar", false, KbdData);
        }
        private void btn_pack_target_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current as App).kbdwnd.Init(StringResource.str("enter_target"), "target", false, KbdData);
        }
        private void btn_pack_uvar_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current as App).kbdwnd.Init(StringResource.str("enter_uvar"), "uvar", false, KbdData);
        }
        private void ShowStatus(string msg)
        {
            txt_oper.Content = StringResource.str(msg);
            txt_oper.Visibility = Visibility.Visible;
            bg_oper.Visibility = Visibility.Visible;

        }
        private List<string> apply_regs = new List<string>();
        private void sub_applyall()
        {
            App p = Application.Current as App;
            UIPacker pack = curr_packer;
            NodeAgent agent = pack.agent;
            byte cn = (byte)curr_node_index;

            bool star = true;
            foreach (byte n in curr_packer.weight_nodes) 
            {
                foreach (string reg in apply_regs.Distinct())
                {
                    if (agent.GetStatus(n) == NodeStatus.ST_LOST)
                        continue;
                    if (agent.GetStatus(n) == NodeStatus.ST_DISABLED)
                        continue;

                    if (n == cn)
                        continue;
                    UInt32 val = UInt32.Parse(curr_packer.agent.GetNodeReg(cn, reg));
                    if (!curr_packer.agent.SetNodeReg(n, reg, val))
                    {
                        return;
                    }

                    if (star)
                    {
                        Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, (Action)delegate
                        {
                            txt_oper.Content = StringResource.str("modifying") +" "+n.ToString()+"*";
                        });

                    }
                    else
                    {
                        Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, (Action)delegate
                        {
                            txt_oper.Content = StringResource.str("modifying") + " " + n.ToString();
                        });
                    }
                    star = !star;
                }
            }
            apply_regs.Clear();   
        }
        private void btn_sub_applyall_Click(object sender, RoutedEventArgs e)
        {
            //apply for other nodes.
            if (lastcall != "")
                return;
            ShowStatus("modifying");
            lastcall = "ApplyToAll";
        }
        public void new_prd_no_input(string param, string data)
        {
            if (curr_packer.pkg_confs.Keys.Contains<string>(data))
            {
                MessageBox.Show(StringResource.str("used_prdno"));
                lastcall = "select_prdno";
            }
            else
            {
                curr_packer.DuplicateCurrentConfig(data);
                ShowStatus("loading");
                lastcall = "UpdateUI";
            }
        }
        private void new_prd_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current as App).kbdwnd.Init(StringResource.str("input_newprdno"), "", false, new_prd_no_input);
        }

        private void btn_ret_config_Click(object sender, RoutedEventArgs e)
        {
            if (curr_packer.status != PackerStatus.RUNNING)
            {
                while (lastcall != "")
                    this.uiTimer_Tick(null, null);
                (Application.Current as App).SwitchTo("configmenu");
            }
        }
        private void btn_ret_run_Click(object sender, RoutedEventArgs e)
        {
            if (curr_packer.status != PackerStatus.RUNNING)
            {
                while (lastcall != "")
                    this.uiTimer_Tick(null, null);
                (Application.Current as App).SwitchTo("runmode");
            }
        }
        private void lbl_currNode_Click(object sender, RoutedEventArgs e)
        {

        }
        private void main_input_click(object sender, RoutedEventArgs e)
        {
            App p = Application.Current as App;
            if (curr_packer.status == PackerStatus.RUNNING)
            {
                //MessageBox.Show("is_running");
                return;
            }
            string regname = (sender as Label).Name;
            p.kbdwnd.Init(StringResource.str("enter_" + regname), regname, false, KbdData);
        }

        private void btn_selectall_Click(object sender, RoutedEventArgs e)
        {
            bSelectAll = !bSelectAll;
            lastcall = "UpdateUI";
            btn_selectall.Style = bSelectAll ? this.FindResource("ButtonStyle2") as Style : this.FindResource("ButtonStyle") as Style;
        }

        private void cb_autoamp_Click(object sender, RoutedEventArgs e)
        {
            if (curr_node_index < 0)
                return;
            App p = Application.Current as App;
            if (!cb_autoamp.IsChecked.Value)
            {
                KbdData("autoamp", "0");
            }
            else
            {
                p.kbdwnd.Init(StringResource.str("enter_autoamp"), "autoamp", false, KbdData);
            }
        }

        private void ellipseWithImageBrush_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            App p = Application.Current as App;
            p.prodwnd.Init(prd_pic_selected);

        }
    }
}
