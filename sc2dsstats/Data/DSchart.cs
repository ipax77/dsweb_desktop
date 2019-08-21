using Microsoft.JSInterop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace sc2dsstats.Data
{
    public class DSchart
    {
        public static ChartJS SortChart(ChartJS chart, ref List<string> s_races_ordered)
        {
            if (chart.type == "radar" || chart.type == "line") return chart;
            List<ChartJSdataset> datasets = new List<ChartJSdataset>(chart.data.datasets);
            List<ChartJSsorthelper> sortedItems = new List<ChartJSsorthelper>();

            if (datasets.Count > 0)
            {
                for (int i = 0; i < chart.data.labels.Count(); i++)
                {
                    if (chart.data.datasets[0].data.Count() > i)
                        sortedItems.Add(new ChartJSsorthelper(chart.data.labels[i], chart.data.datasets[0].data[i]));
                }
                sortedItems = sortedItems.OrderBy(o => o.WR).ToList();
                chart.data.labels = sortedItems.Select(x => x.CMDR).ToArray();
                chart.data.datasets[0].data = sortedItems.Select(x => x.WR).ToArray();

                if (datasets.Count > 1)
                {
                    for (int d = 1; d < datasets.Count(); d++)
                    {
                        List<ChartJSsorthelper> temp_sortedItems = new List<ChartJSsorthelper>();
                        //for (int i = 0; i < DSdata.s_races_cmdr.Count(); i++)
                        for (int i = 0; i < chart.data.datasets[d].data.Count(); i++)
                        {
                            temp_sortedItems.Add(new ChartJSsorthelper(DSdata.s_races_cmdr[i], chart.data.datasets[d].data[i]));
                        }
                        List<ChartJSsorthelper> add_sortedItems = new List<ChartJSsorthelper>();
                        foreach (string label in chart.data.labels)
                        {
                            foreach (ChartJSsorthelper help in temp_sortedItems)
                            {
                                if (label.StartsWith(help.CMDR))
                                {
                                    add_sortedItems.Add(help);
                                }
                            }
                        }
                    }
                }
            }
            List<string> _s_races_ordered = new List<string>();
            foreach (var ent in sortedItems)
            {
                Match m = Regex.Match(ent.CMDR, @"^(\w)+");
                if (m.Success) _s_races_ordered.Add(m.Value);
            }
            s_races_ordered = _s_races_ordered;


            return chart;
        }

        public static ChartJScolorhelper GetChartColor_bak(int myi)
        {
            string temp_col;
            if (myi == 1) temp_col = "26, 94, 203";
            else if (myi == 2) temp_col = "203, 26, 59";
            else if (myi == 2) temp_col = "203, 26, 59";
            else if (myi == 3) temp_col = "47, 203, 26";
            else if (myi == 4) temp_col = "26, 203, 191";
            else if (myi == 5) temp_col = "203, 26, 177";
            else if (myi == 6) temp_col = "203, 194, 26";
            else temp_col = "72, 69, 9";

            ChartJScolorhelper col = new ChartJScolorhelper();
            col.backgroundColor = "rgba(" + temp_col + ", 0.5)";
            col.borderColor = "rgba(" + temp_col + ",1)";
            col.pointBackgroundColor = "rgba(" + temp_col + ", 0.2)";
            return col;
        }

        public static ChartJScolorhelper GetChartColorFromLabel(string cmdr)
        {
            string cmdr_col = DSdata.CMDRcolor[cmdr];
            Color color = ColorTranslator.FromHtml(cmdr_col);
            string temp_col = color.R + ", " + color.G + ", " + color.B;
            ChartJScolorhelper col = new ChartJScolorhelper();
            col.backgroundColor = "rgba(" + temp_col + ", 0.5)";
            col.borderColor = "rgba(" + temp_col + ",1)";
            col.barborderColor = "rgb(255, 0, 0)";
            col.pointBackgroundColor = "rgba(" + temp_col + ", 0.2)";
            return col;
        }

        public static ChartJScolorhelper GetChartColor(int myi)
        {


            string temp_col;
            if (myi == 1) temp_col = "0, 0, 255";
            else if (myi == 2) temp_col = "204, 0, 0";
            else if (myi == 2) temp_col = "0, 153, 0";
            else if (myi == 3) temp_col = "204, 0, 153";
            else if (myi == 4) temp_col = "0, 204, 255";
            else if (myi == 5) temp_col = "255, 153, 0";
            else if (myi == 6) temp_col = "0, 51, 0";
            else if (myi == 7) temp_col = "0, 101, 0";
            else if (myi == 8) temp_col = "0, 151, 0";
            else if (myi == 9) temp_col = "0, 251, 0";
            else if (myi == 10) temp_col = "0, 51, 50";
            else if (myi == 11) temp_col = "0, 51, 100";
            else if (myi == 12) temp_col = "0, 51, 150";
            else if (myi == 13) temp_col = "0, 51, 200";
            else if (myi == 14) temp_col = "0, 51, 250";
            else if (myi == 15) temp_col = "50, 51, 0";
            else temp_col = "102, 51, 0";

            ChartJScolorhelper col = new ChartJScolorhelper();
            col.backgroundColor = "rgba(" + temp_col + ", 0.5)";
            col.borderColor = "rgba(" + temp_col + ",1)";
            col.barborderColor = "rgb(255, 0, 0)";
            col.pointBackgroundColor = "rgba(" + temp_col + ", 0.2)";
            return col;
        }
    }



    public class PlainJsonStringConverter : JsonConverter
    {
        //[JsonConverter(typeof(PlainJsonStringConverter))]
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return reader.Value;
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue((string)value);
        }
    }

    public class ChartJScolorhelper
    {
        public string backgroundColor { get; set; }
        public string borderColor { get; set; }
        public string pointBackgroundColor { get; set; }
        public string barborderColor { get; set; }
    }

    public class ChartJSsorthelper
    {
        public string CMDR { get; set; }
        public double WR { get; set; }

        public ChartJSsorthelper(string _CMDR, double _WR)
        {
            CMDR = _CMDR;
            WR = _WR;
        }
    }

    public class ChartJS
    {
        public string type { get; set; }
        public ChartJSData data { get; set; } = new ChartJSData();
        public ChartJsoptions options { get; set; }
    }

    public class ChartJsoptions
    {
        public bool responsive { get; set; } = true;
        public bool maintainAspectRatio { get; set; } = true;
        public ChartJSoptionsLegend legend { get; set; } = new ChartJSoptionsLegend();
        public ChartJSoptionsTitle title { get; set; } = new ChartJSoptionsTitle();
        
        //public ChartJSoptionsScale scale { get; set; }
    }

    public class ChartJsoptionsBar : ChartJsoptions
    {
        public ChartJSoptionselements elements { get; set; } = new ChartJSoptionselements();
        public ChartJSoptionsplugins plugins { get; set; } = new ChartJSoptionsplugins();
    }

    public class ChartJsoptions0 : ChartJsoptionsBar
    {
        public ChartJSoptionsScales scales { get; set; } = new ChartJSoptionsScales();
    }

    public class ChartJSoptionsScales
    {
        public List<ChartJSoptionsScalesTicks> yAxes { get; set; } = new List<ChartJSoptionsScalesTicks>();
    }

    public class ChartJSoptionsScalesTicks
    {
        public ChartJSoptionsScaleTicks ticks { get; set; } = new ChartJSoptionsScaleTicks();
    }

    public class ChartJsoptionsradar : ChartJsoptions
    {
        public ChartJSoptionsScale scale { get; set; } = new ChartJSoptionsScale();
    }

    public class ChartJSoptionsLegend
    {
        public string position { get; set; } = "top";
        public ChartJSoptionslegendlabels labels { get; set; } = new ChartJSoptionslegendlabels();
    }

    public class ChartJSoptionslegendlabels
    {
        public int fontSize { get; set; } = 14;
        public string fontColor { get; set; } = "#eaffff";
    }

    public class ChartJSoptionsTitle
    {
        public bool display { get; set; } = true;
        public string text { get; set; }
        public int fontSize { get; set; } = 22;
        public string fontColor { get; set; } = "#eaffff";
    }

    public class ChartJSoptionselements
    {
        public ChartJSoptionselementsrectangle rectangle { get; set; } = new ChartJSoptionselementsrectangle();
    }

    public class ChartJSoptionselementsrectangle
    {
        public string backgroundColor { get; set; } = "cc55aa";
    }

    public class ChartJSoptionsScale
    {
        public ChartJSoptionsScaleTicksRadar ticks { get; set; } = new ChartJSoptionsScaleTicksRadar();
        public ChartJSoptionsradargridlines gridLines { get; set; } = new ChartJSoptionsradargridlines();
        public ChartJSoptionsradarangleLines angleLines { get; set; } = new ChartJSoptionsradarangleLines();
        public ChartJSoptionsradarpointLabels pointLabels { get; set; } = new ChartJSoptionsradarpointLabels();
    }

    public class ChartJSoptionsScaleTicks
    {
        public bool beginAtZero { get; set; }
    }

    public class ChartJSoptionsScaleTicksRadar
    {
        public bool display { get; set; } = true;
        public bool beginAtZero { get; set; } = true;
        public string color = "#808080";
        public string backdropColor = "#041326";
    }

    public class ChartJSoptionsradargridlines
    {
        public string color { get; set; } = "#808080";
        public double lineWidth { get; set; } = 0.25;
    }

    public class ChartJSoptionsradarangleLines
    {
        public bool display { get; set; } = true;
        public string color { get; set; } = "#808080";
        public double lineWidth { get; set; } = 0.25;
    }

    public class ChartJSoptionsradarpointLabels
    {
        public int fontSize { get; set; } = 14;
        public string fontColor { get; set; } = "#46a2c9";
    }

        public class ChartJSoptionsplugins
    {
        public ChartJSoptionspluginsdatalabels datalabels { get; set; } = new ChartJSoptionspluginsdatalabels();
        public ChartJSPluginlabels labels { get; set; } = new ChartJSPluginlabels();
    }

    public class ChartJSoptionspluginsdatalabels
    {
        public string color = "#eaffff";
        public string align { get; set; } = "bottom";
        public string anchor { get; set; } = "end";
        //[JsonConverter(typeof(PlainJsonStringConverter))]
        //public string display { get; set; } = "function (context) { return context.dataset.data[context.dataIndex] > 15;";
        public string display { get; set; }
        public ChartJSoptionspluginsdatalabelsfont font { get; set; } = new ChartJSoptionspluginsdatalabelsfont();
        //[JsonConverter(typeof(PlainJsonStringConverter))]
        //public string formatter { get; set; } = "Math.Round";
    }

    public class ChartJSoptionspluginsdatalabelsfont
    {
        public string weight { get; set; } = "bold";
    }

    public class ChartJSData
    {
        public string[] labels { get; set; }
        public List<ChartJSdataset> datasets { get; set; } = new List<ChartJSdataset>();
    }

    public class ChartJSdataset
    {
        public string label { get; set; }
        public List<string> backgroundColor { get; set; } = new List<string>();
        public string borderColor { get; set; }
        public string pointBackgroundColor { get; set; }
        public int borderWidth { get; set; } = 1;
        public double[] data { get; set; }
    }

    public class ChartJSPluginlabels
    {
        public string render { get; set; } = "image";
        public List<ChartJSPluginlabelsImage> images { get; set; } = new List<ChartJSPluginlabelsImage>();
    }

    public class ChartJSPluginlabelsImage
    {
        public string src { get; set; } = "images/dummy.png";
        public int width { get; set; } = 45;
        public int height { get; set; } = 45;
    }

    public class PieChart
    {
        public List<double> piedata { get; set; } = new List<double>();
        public List<string> pielabels { get; set; } = new List<string>();
        public List<string> piecolors { get; set; } = new List<string>();
    }

    public class JsInteropClasses
    {
        private readonly IJSRuntime _jsRuntime;

        public JsInteropClasses(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<string> ChartChanged(string data)
        {
            // The handleTickerChanged JavaScript method is implemented
            // in a JavaScript file, such as 'wwwroot/tickerJsInterop.js'.
            return await _jsRuntime.InvokeAsync<string>("DynChart", data);
        }

        public async Task<string> AddDataset(string data)
        {
            return await _jsRuntime.InvokeAsync<string>("AddDynChart", data);
        }

        public async Task<string> RemoveDataset(int data)
        {
            return await _jsRuntime.InvokeAsync<string>("RemoveDynChart", data);
        }
    }

    public class ChartStateChange : INotifyPropertyChanged
    {
        private bool Update_value = false;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Models.dsfilter Fil { get; set; } = new Models.dsfilter();

        public bool Update
        {
            get { return this.Update_value; }
            set
            {
                if (value != this.Update_value)
                {
                    this.Update_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }
}
