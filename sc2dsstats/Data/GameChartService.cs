using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace sc2dsstats.Data
{
    public class GameChartService
    {
        private readonly IJSRuntime _jsRuntime;
        private JsInteropClasses _jsIterop;
        public ChartJS mychart { get; set; } = new ChartJS();

        public List<string> colorPool = new List<string>()
            {
                "0, 0, 255",
                "204, 0, 0",
                "0, 153, 0",
                "204, 0, 153",
                "0, 204, 255",
                "255, 153, 0",
                "0, 51, 0",
                "0, 101, 0",
                "0, 151, 0",
                "0, 251, 0",
                "0, 51, 50",
                "0, 51, 100",
                "0, 51, 150",
                "0, 51, 200",
                "0, 51, 250",
                "50, 51, 0",
            };

        List<string> mycolorPool;
        Regex rx_col = new Regex(@"^rgba\((\d+, \d+, \d+)");



        JsonWriterOptions jOption = new JsonWriterOptions()
        {
            Indented = true
        };

        public GameChartService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
            _jsIterop = new JsInteropClasses(_jsRuntime);
            mycolorPool = new List<string>(colorPool);
        }

        public async Task<ChartJS> GetChartBase(bool draw = true)
        {
            mychart = new ChartJS();
            mychart.type = "line";
            mychart.options = GetOptions();
            mychart.options.title.text = "game details";
            mychart.options.title.fontColor = "#0c07ad";
            mychart.options.legend.labels.fontColor = "#0c07ad";
            if (draw == true) await _jsIterop.ChartChanged(JsonSerializer.Serialize(mychart));
            mycolorPool = new List<string>(colorPool);
            return mychart;
        }

        public async Task<ChartJS> AddDataset(ChartJSdataset dataset)
        {
            var col = GetRandomChartColor();
            dataset.backgroundColor.Add("rgba(0, 0, 0, 0)");
            dataset.borderColor = col.borderColor;
            dataset.pointBackgroundColor = col.pointBackgroundColor;
            mychart.data.datasets.Add(dataset);
            await _jsIterop.AddDataset(JsonSerializer.Serialize(dataset));
            return mychart;
        }

        public async Task<ChartJS> RemoveDataset(string label)
        {
            if (mychart.data.datasets.Count() == 0) return mychart;
            int i = mychart.data.datasets.Count() - 1;
            try
            {
                i = mychart.data.datasets.FindIndex(x => x.label == label);
                string col = mychart.data.datasets[i].borderColor;
                Match m = rx_col.Match(col);
                if (m.Success)
                    mycolorPool.Add(m.Groups[1].Value.ToString());

                mychart.data.datasets.RemoveAt(i);
            }
            catch { }
            await _jsIterop.RemoveDataset(i);
            return mychart;
        }

        public async Task DrawChart(ChartJS chart)
        {
            mychart = chart;
            await _jsIterop.ChartChanged(JsonSerializer.Serialize(mychart));
        }

        public ChartJsoptions GetOptions()
        {
            ChartJsoptions chartoptions = new ChartJsoptions();

            ChartJsoptions0 zoptions = new ChartJsoptions0();

            ChartJSoptionsScales scales = new ChartJSoptionsScales();
            ChartJSoptionsScalesTicks sticks = new ChartJSoptionsScalesTicks();
            ChartJSoptionsScaleTicks ticks = new ChartJSoptionsScaleTicks();
            ticks.beginAtZero = true;
            sticks.ticks = ticks;
            scales.yAxes.Add(sticks);
            zoptions.scales = scales;
            chartoptions = zoptions;
            chartoptions.title.display = true;
            return chartoptions;
        }


        public ChartJScolorhelper GetRandomChartColor()
        {
            Random rnd = new Random();

            string temp_col = "50, 51, 0";

            if (mycolorPool.Count() > 0)
            {
                int iCol = rnd.Next(0, mycolorPool.Count());
                temp_col = mycolorPool[iCol];
                mycolorPool.RemoveAt(iCol);
            }
            ChartJScolorhelper col = new ChartJScolorhelper();
            col.backgroundColor = "rgba(" + temp_col + ", 0.5)";
            col.borderColor = "rgba(" + temp_col + ",1)";
            col.barborderColor = "rgb(255, 0, 0)";
            col.pointBackgroundColor = "rgba(" + temp_col + ", 0.2)";
            return col;
        }
    }

}

