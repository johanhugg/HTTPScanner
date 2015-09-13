﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HTTPScanner
{
    public partial class ScanForm : Form
    {
        private bool scanning = false;
        private Scanner scanner;
        private int maxNumOfAsyncScanners = 200;
        private CancellationTokenSource cancellationTokenSource;

        public ScanForm()
        {
            InitializeComponent();
            scanner = new Scanner();
        }

        private async void startScanButton_Click(object sender, EventArgs e)
        {
            startScanButton.Enabled = false;
            scanning = true;
            while (scanning)
            {
                cancellationTokenSource = new CancellationTokenSource();
                var taskList = new List<Task<HttpResponseMessage>>();

                IEnumerable<Task<HttpResponseMessage>> enumerableTasks = from value in Enumerable.Range(0, maxNumOfAsyncScanners)
                                                                            select scanner.ScanIPAddressAsync(Scanner.GenerateIPAddress(), cancellationTokenSource.Token);
                var tasks = enumerableTasks.ToArray();
                var res = await Task.WhenAll(tasks);
                Console.WriteLine("All done");
                var httpResponseMessages = new List<HttpResponseMessage>();
                foreach (var v in tasks)
                {
                    if (v.Result == null)
                        continue;
                    httpResponseMessages.Add(v.Result);
                }
                FilterAndAddResponseMessages(httpResponseMessages);
                cancellationTokenSource = null;
            }
            startScanButton.Enabled = true;
        }

        private List<HttpStatusCode> BuildFilter()
        {
            var statusCodes = new List<HttpStatusCode>();

            if (anyHttpStatusCheckbox.Checked)
            {
                foreach (HttpStatusCode code in Enum.GetValues(typeof(HttpStatusCode)))
                    statusCodes.Add(code);
                return statusCodes;
            }
            if (okHttpStatusCheckbox.Checked)
                statusCodes.Add(HttpStatusCode.OK);
            if (badRequestHttpStatusCheckbox.Checked) 
                statusCodes.Add(HttpStatusCode.BadRequest);
            if (unauthorizedHttpStatusCheckbox.Checked)
                statusCodes.Add(HttpStatusCode.Unauthorized);

            return statusCodes;
        }

        private void FilterAndAddResponseMessages(List<HttpResponseMessage> msgs)
        {
            var acceptedCodes = BuildFilter();
            
            foreach (var msg in msgs)
            {
                if (acceptedCodes.Contains(msg.StatusCode))
                {
                    var address = msg.RequestMessage.RequestUri.Host.ToString();
                    var statuscode = msg.StatusCode.ToString();
                    var item = new ListViewItem(new string[] { address, statuscode });
                    resultList.Items.Add(item);
                    resultList.EnsureVisible(resultList.Items.Count - 1);
                }
            }
        }

        private void stopScanButton_Click(object sender, EventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                scanning = false;
            }
        }


        private void resultList_DoubleClick(object sender, EventArgs e)
        {
            var clickedItem = resultList.SelectedItems[0].SubItems[0].Text;
            System.Diagnostics.Process.Start("http://" + clickedItem);
        }
    }
}
