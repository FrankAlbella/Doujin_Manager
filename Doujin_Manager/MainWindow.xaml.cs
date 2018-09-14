﻿using Doujin_Manager.Util;
using Doujin_Manager.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace Doujin_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CentralViewModel dataContext;
        Thread populateThread;

        public MainWindow()
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine(System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath);
        }

        private void AsyncPopulateDoujinPanel()
        {
            DoujinScrubber ds = new DoujinScrubber(dataContext);

            if (populateThread != null && populateThread.IsAlive)
                populateThread.Abort();

            populateThread = new Thread(() => ds.PopulateDoujins());
            populateThread.Name = "DoujinScrubber Thread";
            populateThread.IsBackground = true;
            populateThread.Start();
        }

        private void ChooseDoujinRootDirection()
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (!string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    Properties.Settings.Default.DoujinDirectory = fbd.SelectedPath;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dataContext = (CentralViewModel)this.DataContext;

            // If appdata folder doesn't exist, create it and the thumbnail folder
            if (!Directory.Exists(PathUtil.appdataDir))
                Directory.CreateDirectory(PathUtil.thumbnailDir);

            // Open folder select dialog if no folder location is saved
            if (!Directory.Exists(Properties.Settings.Default.DoujinDirectory))
            {
                ChooseDoujinRootDirection();
            }
        }

        private void doujinListView_Loaded(object sender, RoutedEventArgs e)
        {
            AsyncPopulateDoujinPanel();
        }

        private void btnChangeDirectory_Click(object sender, RoutedEventArgs e)
        {
            ChooseDoujinRootDirection();
            dataContext.DoujinsViewModel.Doujins.Clear();
            AsyncPopulateDoujinPanel();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            dataContext.DoujinsViewModel.Doujins.Clear();
            AsyncPopulateDoujinPanel();
        }

        // TODO: causes an exception because images are not released after list is cleared
        private void btnCache_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < dataContext.DoujinsViewModel.Doujins.Count; i++)
            {
                dataContext.DoujinsViewModel.Doujins[i] = null;
            }

            dataContext.DoujinsViewModel.Doujins.Clear();
            GC.Collect();
            string[] thumbsnails = Directory.GetFiles(PathUtil.thumbnailDir);

            foreach (string thumbnail in thumbsnails)
            {
                File.Delete(thumbnail);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Cache cache = new Cache();
            cache.Save(new List<Doujin>(this.dataContext.DoujinsViewModel.Doujins));
        }
    }
}