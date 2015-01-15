using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CheckReplace
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private class Log
        {
            public Log(string log)
            {
                this.log = log;
            }

            public string log { get; set; }
        }
        private ObservableCollection<Log> logList = new ObservableCollection<Log>();

        private class Repeat
        {
            public string origin { get; set; }
            public string local { get; set; }
            public string text { get; set; }
        }
        private ObservableCollection<Repeat> repeatText = new ObservableCollection<Repeat>();

        public class Text
        {
            public string index { get; set; }
            public string local { get; set; }
            public string text { get; set; }
        }

        private List<Text> strList = new List<Text>();


        public MainWindow()
        {
            InitializeComponent();
            LogField.ItemsSource = logList;
            RepeatField.ItemsSource = repeatText;
        }

        public System.Text.Encoding GetType(FileStream fs)
        {
            /*byte[] Unicode=new byte[]{0xFF,0xFE}; 
            byte[] UnicodeBIG=new byte[]{0xFE,0xFF}; 
            byte[] UTF8=new byte[]{0xEF,0xBB,0xBF};*/

            BinaryReader r = new BinaryReader(fs, System.Text.Encoding.Default);
            byte[] ss = r.ReadBytes(3);
            r.Close();
            //编码类型 Coding=编码类型.ASCII;  
            if (ss[0] >= 0xEF)
            {
                if (ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF)
                {
                    return System.Text.Encoding.UTF8;
                }
                else if (ss[0] == 0xFE && ss[1] == 0xFF)
                {
                    return System.Text.Encoding.BigEndianUnicode;
                }
                else if (ss[0] == 0xFF && ss[1] == 0xFE)
                {
                    return System.Text.Encoding.Unicode;
                }
                else
                {
                    return System.Text.Encoding.Default;
                }
            }
            else
            {
                return System.Text.Encoding.Default;
            }
        }

        private void Browser_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Description = "选择路径";
            dlg.RootFolder = Environment.SpecialFolder.Desktop;
            dlg.SelectedPath = Environment.SpecialFolder.MyDocuments.ToString();
            dlg.ShowNewFolderButton = true;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PathStr.Text = dlg.SelectedPath;
            }

        }


        private async void Confirm_Click(object sender, RoutedEventArgs e)
        {
            logList.Clear();
            repeatText.Clear();
            strList.Clear();
            if (PathStr.Text == "")
            {
                logList.Add(new Log("没有选择任何路径。"));
                return;
            }
            logList.Add(new Log("开始处理..."));
            DirectoryInfo di = new DirectoryInfo(PathStr.Text);
            var files = di.GetFiles("*.txt");
            logList.Add(new Log(string.Format("共有 {0} 个文本文件。", files.Count())));
            foreach (var file in files)
            {
                logList.Add(new Log(string.Format("正在处理文件...{0}", file.Name)));
                await DealTextAsync(file.FullName, file.Name.Replace(file.Extension,""));
                logList.Add(new Log(string.Format("{0} 处理结束", file.Name)));
                
            }
        }

        private async Task DealTextAsync(string path, string filename)
        {
            await Task.Run(() =>
            {
                string text = "";
                string index = "";
                int lastnumber = strList.Count; 
                FileStream stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
                Encoding code = GetType(stream);
                stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
                StreamReader reader = new StreamReader(stream, code);
                long i = 0;
                string last = "";
                string val = "";
                bool first = true;
                while (!reader.EndOfStream)
                {
                    last = val;
                    val = reader.ReadLine();
                    if (val.StartsWith("##") != true && val != "")
                    {
                        text += val + System.Environment.NewLine;
                    }
                    else if (val == ""&&last!="")
                    {
                        
                    }
                    else if (val.StartsWith("##"))
                    {
                        if (!first)
                        {
                            if (strList.Count > 1)
                            {
                                var result = strList.Where<Text>(it => it.text == text);
                                if (result.Count() > 0)
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        repeatText.Add(new Repeat() { text = text, origin = string.Format("{0}-{1}", filename, i), local = result.First().local });
                                    }, System.Windows.Threading.DispatcherPriority.Normal);
                                    index += System.Environment.NewLine + "〓" + result.First().local;
                                }
                            }
                            strList.Add(new Text() { local = string.Format("{0}-{1}", filename, i), text = text, index = index });
                        }
                        i++;
                        index = val;
                        first = false;
                        text = "";

                    }
                }
                if (strList.Count > 1)
                {
                    var result = strList.Where<Text>(it => it.text == text);
                    if (result.Count() > 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            repeatText.Add(new Repeat() { text = text, origin = string.Format("{0}-{1}", filename, i), local = result.First().local });
                        }, System.Windows.Threading.DispatcherPriority.Normal);
                        index += System.Environment.NewLine + "〓" + result.First().local;
                    }
                }
                strList.Add(new Text() { local = string.Format("{0}-{1}", filename, i), text = text, index = index });

                reader.Close();
                string strPath = "replace\\" + filename + ".txt";
                System.IO.Directory.CreateDirectory("replace");
                using (FileStream outStream = new FileStream(strPath,FileMode.Create,FileAccess.ReadWrite))
                {
                    StreamWriter writer = new StreamWriter(outStream, code);
                    for (int k = lastnumber; k < strList.Count;k++ )
                    {
                        writer.WriteLine(strList[k].index);
                        writer.WriteLine(strList[k].text);
                        writer.WriteLine();
                        writer.Flush();
                    }
                }
            });
            
        }
    }
}
