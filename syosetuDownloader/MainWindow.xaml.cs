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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using HtmlAgilityPack;

namespace syosetuDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int _row = 0;
        string _link = String.Empty;
        string _start = String.Empty;
        string _end = String.Empty;
        string _format = String.Empty;
        Syousetsu.Constants.FileType _fileType;
        List<Syousetsu.Controls> _controls = new List<Syousetsu.Controls>();

        public MainWindow()
        {
            InitializeComponent();
            using (System.IO.StreamReader sr = new System.IO.StreamReader("format.ini"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (!line.StartsWith(";"))
                    {
                        _format = line;
                        break;
                    }
                }
            }
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            _row += 1;

            this._link = txtLink.Text;
            this._start = txtFrom.Text;
            this._end = txtTo.Text;

            if (_link != String.Empty)
            {
                if (!_link.StartsWith("http")) { _link = @"http://" + _link; }
                if (!_link.EndsWith("/")) { _link = _link + "/"; }

                Syousetsu.Constants sc = new Syousetsu.Constants();
                HtmlDocument toc = Syousetsu.Methods.GetTableOfContents(_link, sc.SyousetsuCookie);
                if (Syousetsu.Methods.IsValidLink(_link) &&
                    Syousetsu.Methods.IsValid(toc))
                {
                    Label lb = new Label();
                    lb.Content = Syousetsu.Methods.GetTitle(toc);
                    lb.ToolTip = "Click to open folder";

                    ProgressBar pb = new ProgressBar();
                    pb.Maximum = (_end == String.Empty) ? Syousetsu.Methods.GetTotalChapters(toc) : Convert.ToDouble(_end);
                    pb.ToolTip = "Click to stop download";
                    pb.Height = 10;

                    Separator s = new Separator();
                    s.Height = 5;

                    sc.Title = lb.Content.ToString();
                    sc.Link = _link;
                    sc.Start = _start;
                    sc.End = _end;
                    sc.CurrentFileType = _fileType;
                    sc.SeriesCode = Syousetsu.Methods.GetSeriesCode(_link);
                    sc.FilenameFormat = _format;

                    System.Threading.CancellationTokenSource ct = Syousetsu.Methods.AddDownloadJob(sc, pb);
                    pb.MouseDown += (snt, evt) =>
                    {
                        ct.Cancel();
                        pb.ToolTip = "";
                    };
                    lb.MouseDown += (snt, evt) =>
                    {
                        string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), sc.Title);
                        if (System.IO.Directory.Exists(path))
                        {
                            System.Diagnostics.Process.Start("explorer.exe", path);
                        }
                    };

                    stackPanel1.Children.Add(lb);
                    stackPanel1.Children.Add(pb);
                    stackPanel1.Children.Add(s);

                    _controls.Add(new Syousetsu.Controls { ID = _row, Label = lb, ProgressBar = pb, Separator = s });
                }
            }
        }

        private void rbText_Checked(object sender, RoutedEventArgs e)
        {
            _fileType = Syousetsu.Constants.FileType.Text;
        }

        private void rbHtml_Checked(object sender, RoutedEventArgs e)
        {
            _fileType = Syousetsu.Constants.FileType.HTML;
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            _controls.Where((c) => c.ProgressBar.Value == c.ProgressBar.Maximum).ToList().ForEach((c) =>
            {
                stackPanel1.Children.Remove(c.Label);
                stackPanel1.Children.Remove(c.ProgressBar);
                stackPanel1.Children.Remove(c.Separator);
            });

            _controls = (from c in _controls
                         where c.ProgressBar.Value != c.ProgressBar.Maximum
                         select c).ToList();
        }
    }
}
