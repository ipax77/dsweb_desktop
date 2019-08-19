using sc2dsstats.Models;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace sc2dsstats.Data
{
    public class ChartService
    {
        private readonly Interfaces.IDSdata_cache _dsdata;
        private DSdyn_filteroptions _options;
        private readonly IJSRuntime _jsRuntime;
        private JsInteropClasses _jsIterop;

        private List<string> s_races_ordered = new List<string>(DSdata.s_races_cmdr);

        public ChartService(Interfaces.IDSdata_cache dsdata, IJSRuntime jsRuntime, DSdyn_filteroptions options)
        {
            _dsdata = dsdata;
            _jsRuntime = jsRuntime;
            _options = options;
            _jsIterop = new JsInteropClasses(_jsRuntime);
            CultureInfo.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
        }

        public void GetChartBase(bool draw = true)
        {
            ChartJS mychart = new ChartJS();
            s_races_ordered = DSdata.s_races_cmdr.ToList();
            mychart.type = "bar";
            if (_options.Mode == "Synergy" || _options.Mode == "AntiSynergy") mychart.type = "radar";
            else if (_options.Mode == "Timeline")
            {
                mychart.type = "line";
                List<string> _s_races_cmdr_ordered = new List<string>();
                DateTime startdate = _options.Startdate;
                DateTime enddate = _options.Enddate;
                DateTime breakpoint = startdate;
                while (DateTime.Compare(breakpoint, enddate) < 0)
                {
                    breakpoint = breakpoint.AddDays(7);
                    _s_races_cmdr_ordered.Add(breakpoint.ToString("yyyy-MM-dd"));
                }
                _s_races_cmdr_ordered.RemoveAt(_s_races_cmdr_ordered.Count() - 1);
                s_races_ordered = _s_races_cmdr_ordered;
            }

            GetData(mychart);
            mychart.options = GetOptions();
            if (mychart.type != "line") DSchart.SortChart(mychart, ref s_races_ordered);
            SetColor(mychart);
            SetCmdrPics(mychart);
            _options.Chart = mychart;
            if (draw == true) _jsIterop.ChartChanged(JsonConvert.SerializeObject(_options.Chart, Formatting.Indented));
        }

        public void AddDataset()
        {
            ChartJS oldchart = new ChartJS();
            oldchart = _options.Chart;
            if (oldchart.data.datasets.Count() == 1 && oldchart.data.datasets[0].label == "global")
            {
                oldchart.data.datasets.RemoveAt(0);
                GetData(oldchart);
                //if (oldchart.type == "bar") oldchart.options.title.text = oldchart.options.title.text + " - " + _options.Interest + " vs ...";
                DSchart.SortChart(oldchart, ref s_races_ordered);
                SetColor(oldchart);
                SetCmdrPics(oldchart);
                _options.Chart = oldchart;
                _jsIterop.ChartChanged(JsonConvert.SerializeObject(_options.Chart, Formatting.Indented));
            } else
            {
                oldchart.data.datasets.Add(GetData());
                SetColor(oldchart);
                _options.Chart = oldchart;
                _jsIterop.AddDataset(JsonConvert.SerializeObject(_options.Chart.data.datasets[_options.Chart.data.datasets.Count() -1], Formatting.Indented));
            }
        }

        public void RemoveDataset()
        {
            ChartJS oldchart = new ChartJS();
            oldchart = _options.Chart;
            if (oldchart.data.datasets.Count() == 1)
            {
                _options.Interest = "";
                GetChartBase();
            } else
            {
                for (int i = 0; i < _options.Chart.data.datasets.Count(); i++)
                {
                    if (_options.Chart.data.datasets[i].label == _options.Interest)
                    {
                        _options.Chart.data.datasets.RemoveAt(i);
                        _jsIterop.RemoveDataset(i);
                        break;
                    }
                }
            }
        }

        public void RebuildChart()
        {
            ChartJS oldchart = new ChartJS();
            oldchart = _options.Chart;
            _options.Interest = "";

            GetChartBase(false);
            
            if (oldchart.data.datasets.Count() == 1 && oldchart.data.datasets[0].label == "global")
            {
                _jsIterop.ChartChanged(JsonConvert.SerializeObject(_options.Chart, Formatting.Indented));
            } else
            {
                _options.Chart.data.datasets.RemoveAt(0);
                ChartJS labelChart = new ChartJS();
                foreach (var ent in oldchart.data.datasets)
                {
                    _options.Interest = ent.label;
                    _options.Chart.data.datasets.Add(GetData(labelChart));
                }
                _options.Chart.data.labels = labelChart.data.labels;
                DSchart.SortChart(_options.Chart, ref s_races_ordered);
                SetColor(_options.Chart);
                SetCmdrPics(_options.Chart);
                _jsIterop.ChartChanged(JsonConvert.SerializeObject(_options.Chart, Formatting.Indented));
            }
        }

        public void SetColor(ChartJS mychart)
        {
            int i = 0;
            foreach (var ent in mychart.data.datasets)
            {
                i++;
                var col = DSchart.GetChartColorFromLabel(ent.label);
                if (mychart.type == "bar")
                {
                    if (ent.label == "global")
                    {
                        foreach (var cmdr in mychart.data.labels)
                        {
                            Match m = Regex.Match(cmdr, @"^(\w)+");
                            if (m.Success)
                            {
                                var col_global = DSchart.GetChartColorFromLabel(m.Value);
                                ent.backgroundColor.Add(col_global.backgroundColor);
                            }
                        }
                    }
                    else
                    {
                        ent.backgroundColor.Add(col.backgroundColor);
                    }
                    ent.borderColor = col.barborderColor;
                    ent.borderWidth = 1;
                }
                else if (mychart.type == "line")
                {
                    ent.backgroundColor.Add("rgba(0, 0, 0, 0)");
                    ent.borderColor = col.borderColor;
                    ent.pointBackgroundColor = col.pointBackgroundColor;
                }
                else if (mychart.type == "radar")
                {
                    ent.backgroundColor.Add(col.pointBackgroundColor);
                    ent.borderColor = col.borderColor;
                    ent.pointBackgroundColor = col.pointBackgroundColor;
                }
            }
        }

        public ChartJsoptions GetOptions()
        {
            ChartJsoptions chartoptions = new ChartJsoptions();


            if (_options.Mode == "Synergy" || _options.Mode == "AntiSynergy")
            {
                ChartJsoptionsradar radaroptions = new ChartJsoptionsradar();
                radaroptions.title.text = _options.Mode;
                radaroptions.legend.position = "bottom";
                if (_options.Player == true) radaroptions.title.text = "Player " + radaroptions.title.text;
                chartoptions = radaroptions;

            }
            else
            {
                if (_options.BeginAtZero == true)
                {
                    ChartJsoptions0 zoptions = new ChartJsoptions0();

                    ChartJSoptionsScales scales = new ChartJSoptionsScales();
                    ChartJSoptionsScalesTicks sticks = new ChartJSoptionsScalesTicks();
                    ChartJSoptionsScaleTicks ticks = new ChartJSoptionsScaleTicks();
                    ticks.beginAtZero = true;
                    sticks.ticks = ticks;
                    scales.yAxes.Add(sticks);
                    zoptions.scales = scales;
                    chartoptions = zoptions;
                } else
                {
                    ChartJsoptionsBar baroptions = new ChartJsoptionsBar();
                    chartoptions = baroptions;                
                }
            }

            chartoptions.title.display = true;
            chartoptions.title.text = _options.Mode;
            if (_options.Player == true) chartoptions.title.text = "Player " + chartoptions.title.text;
            return chartoptions;
        }

        public ChartJSdataset GetData(ChartJS mychart = null)
        {
            Dictionary<string, KeyValuePair<double, int>> winrate = new Dictionary<string, KeyValuePair<double, int>>();
            Dictionary<string, Dictionary<string, KeyValuePair<double, int>>> winratevs = new Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>();

            List<string> labels = new List<string>();
            List<double> wr = new List<double>();

            string info;
            _dsdata.GetDynData(_options, out winrate, out winratevs, out info);

            ChartJSdataset dataset = new ChartJSdataset();
            if (_options.Interest == "")
            {
                foreach (string race in s_races_ordered)
                {
                    if (winrate.ContainsKey(race) && winrate[race].Value > 0)
                    {
                        wr.Add(winrate[race].Key);
                        labels.Add(race + " (" + winrate[race].Value.ToString() + ")");
                    }
                    else
                    {
                        //wr.Add(0);
                        //labels.Add(race + " (0)");
                    }
                }
                dataset.label = "global";
            } else
            {
                foreach (string race in s_races_ordered)
                {
                    if (winratevs[_options.Interest].ContainsKey(race) && winratevs[_options.Interest][race].Value > 0)
                    {
                        wr.Add(winratevs[_options.Interest][race].Key);
                        labels.Add(race + " (" + winratevs[_options.Interest][race].Value.ToString() + ")");
                    }
                    else
                    {
                        //wr.Add(0);
                        //labels.Add(race + "(0)");
                    }
                }
                dataset.label = _options.Interest;
            }
            dataset.data = wr.ToArray();
            if (mychart != null)
            {
                mychart.data.labels = labels.ToArray();
                mychart.data.datasets.Add(dataset);
            }
            return dataset;
        }

        public static void SetCmdrPics(ChartJS chart)
        {
            if (chart.type != "bar") return;

            List<ChartJSPluginlabelsImage> images = new List<ChartJSPluginlabelsImage>();
            foreach (string lcmdr in chart.data.labels)
            {
                foreach (string cmdr in DSdata.s_races_cmdr)
                {
                    if (lcmdr.StartsWith(cmdr))
                    {
                        ChartJSPluginlabelsImage myimage = new ChartJSPluginlabelsImage();
                        myimage.src = "images/btn-unit-hero-" + cmdr.ToLower() + ".png";
                        images.Add(myimage);
                    }
                }
            }
            ChartJsoptionsBar opt = new ChartJsoptionsBar();
            opt = chart.options as ChartJsoptionsBar;
            opt.plugins.labels.images = images;
            chart.options = opt;

        }
    }
}
