// 设备管理大屏图表
(function () {
  var style = getComputedStyle(document.documentElement);
  var accent = style.getPropertyValue('--accent').trim();
  var accent2 = style.getPropertyValue('--accent2').trim();
  var ink = style.getPropertyValue('--ink').trim();
  var muted = style.getPropertyValue('--muted').trim();
  var warn = style.getPropertyValue('--warn').trim();
  var danger = style.getPropertyValue('--danger').trim();
  var ok = style.getPropertyValue('--ok').trim();

  var CN = 'Microsoft YaHei, PingFang SC, sans-serif';
  var NUM = 'Tektur, Microsoft YaHei, monospace';

  var tooltipBase = {
    appendToBody: true,
    backgroundColor: 'rgba(4, 14, 24, 0.94)',
    borderColor: 'rgba(38, 226, 255, 0.32)',
    borderWidth: 1,
    padding: [8, 12],
    textStyle: { color: ink, fontSize: 12, fontFamily: CN },
    extraCssText: 'box-shadow: 0 8px 24px rgba(0,0,0,.5);'
  };

  var stage = document.getElementById('stage');
  function fitStage() {
    if (window.innerWidth <= 900) {
      stage.style.transform = 'none';
      return;
    }
    var s = Math.min(window.innerWidth / 1920, window.innerHeight / 1080);
    var x = (window.innerWidth - 1920 * s) / 2;
    var y = (window.innerHeight - 1080 * s) / 2;
    stage.style.transform = 'translate(' + x + 'px,' + y + 'px) scale(' + s + ')';
  }
  fitStage();

  var charts = [];

  // ---------- 01 设备综合效率：哑铃图（目标 ←→ 本月） ----------
  var dims = ['稳定性', '敏捷', '安全', '维护', '效率', '利用率'];
  var goal = [90, 88, 95, 90, 90, 92];
  var now = [88, 82, 94, 86, 84.6, 90.3];

  var oee = echarts.init(document.getElementById('chart-oee'), null, { renderer: 'svg' });
  oee.setOption({
    animation: false,
    grid: { left: 54, right: 66, top: 6, bottom: 4 },
    tooltip: Object.assign({}, tooltipBase, {
      trigger: 'item',
      formatter: function (p) {
        var i = p.dataIndex;
        return dims[i] + '<br/>本月 ' + now[i] + ' ｜ 目标 ' + goal[i];
      }
    }),
    xAxis: { type: 'value', min: 60, max: 100, show: false },
    yAxis: {
      type: 'category',
      data: dims,
      axisLine: { show: false },
      axisTick: { show: false },
      axisLabel: { color: 'rgba(206, 232, 248, 0.88)', fontSize: 11.5, fontFamily: CN, margin: 12 }
    },
    series: [
      {
        type: 'bar',
        barWidth: 3,
        silent: true,
        itemStyle: { color: 'rgba(120, 170, 210, 0.10)', borderRadius: 2 },
        data: [100, 100, 100, 100, 100, 100]
      },
      {
        type: 'custom',
        silent: true,
        z: 1,
        renderItem: function (params, api) {
          var y = api.coord([0, api.value(0)])[1];
          var x1 = api.coord([api.value(1), api.value(0)])[0];
          var x2 = api.coord([api.value(2), api.value(0)])[0];
          return {
            type: 'line',
            shape: { x1: x1, y1: y, x2: x2, y2: y },
            style: { stroke: 'rgba(140, 190, 225, 0.38)', lineWidth: 2 }
          };
        },
        data: dims.map(function (d, i) { return [i, goal[i], now[i]]; })
      },
      {
        name: '目标',
        type: 'scatter',
        symbolSize: 7,
        itemStyle: { color: 'rgba(150, 190, 220, 0.65)' },
        data: goal
      },
      {
        name: '本月',
        type: 'scatter',
        symbolSize: 11,
        itemStyle: {
          color: accent,
          shadowBlur: 14,
          shadowColor: 'rgba(38, 226, 255, 0.85)',
          borderColor: 'rgba(4, 14, 24, 0.9)',
          borderWidth: 1.5
        },
        label: {
          show: true, position: 'right', distance: 9,
          color: 'rgba(214, 240, 255, 0.92)', fontSize: 10.5, fontFamily: NUM,
          formatter: '{c}'
        },
        data: now,
        z: 3
      }
    ]
  });
  charts.push(oee);

  // ---------- 02 设备负载与异常预警：双折线 + 预警标记 ----------
  var devices = ['一混粉碎机', '预混粉碎机', '一混配料秤', '二混混合机', '天然气烘干', '发酵包装线', '码垛机器人', '除尘风机'];
  var current = [78, 64, 86, 82, 91, 74, 68, 57];
  var peak = [92, 81, 96, 94, 98, 88, 79, 72];
  var alertIndex = { 0: '高风险 · 轴承温度偏高', 4: '中风险 · 出风温度波动', 3: '中风险 · 混合均匀度偏低', 7: '中风险 · 滤袋压差升高' };

  var load = echarts.init(document.getElementById('chart-load'), null, { renderer: 'svg' });
  load.setOption({
    animation: false,
    grid: { left: 42, right: 20, top: 16, bottom: 54 },
    tooltip: Object.assign({}, tooltipBase, {
      trigger: 'axis',
      formatter: function (ps) {
        var i = ps[0].dataIndex;
        var s = devices[i] + '<br/>当前负载 ' + current[i] + '%<br/>峰值负载 ' + peak[i] + '%';
        if (alertIndex[i]) s += '<br/><span style="color:' + warn + '">预警：' + alertIndex[i] + '</span>';
        return s;
      }
    }),
    xAxis: {
      type: 'category',
      data: devices,
      boundaryGap: false,
      axisLine: { lineStyle: { color: 'rgba(70, 160, 220, 0.22)' } },
      axisTick: { show: false },
      axisLabel: {
        color: 'rgba(186, 214, 234, 0.85)', fontSize: 10, fontFamily: CN,
        interval: 0, rotate: 32, margin: 10
      }
    },
    yAxis: {
      type: 'value', min: 40, max: 100, splitNumber: 4,
      axisLine: { show: false },
      axisTick: { show: false },
      axisLabel: { color: muted, fontSize: 10, fontFamily: NUM, formatter: '{value}%' },
      splitLine: { lineStyle: { color: 'rgba(70, 160, 220, 0.10)', type: 'dashed' } }
    },
    series: [
      {
        name: '峰值负载',
        type: 'line',
        smooth: true,
        symbol: 'circle',
        symbolSize: 4,
        lineStyle: { width: 1.5, type: 'dashed', color: 'rgba(61, 123, 255, 0.75)' },
        itemStyle: { color: 'rgba(61, 123, 255, 0.9)' },
        data: peak
      },
      {
        name: '当前负载',
        type: 'line',
        smooth: true,
        symbol: 'circle',
        symbolSize: 5,
        lineStyle: { width: 2, color: accent, shadowBlur: 10, shadowColor: 'rgba(38, 226, 255, 0.4)' },
        itemStyle: { color: accent, borderColor: 'rgba(4, 14, 24, 0.9)', borderWidth: 1 },
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(38, 226, 255, 0.22)' },
            { offset: 1, color: 'rgba(38, 226, 255, 0)' }
          ])
        },
        data: current,
        markPoint: {
          symbol: 'circle',
          symbolSize: 9,
          itemStyle: { color: danger, shadowBlur: 14, shadowColor: 'rgba(255, 84, 112, 0.8)' },
          label: { show: false },
          data: [{ coord: [0, 78] }, { coord: [4, 91] }]
        }
      }
    ]
  });
  charts.push(load);

  // ---------- 03 设备状态构成：横向条形 ----------
  var statusNames = ['运行', '待机', '检修', '故障'];
  var statusValues = [23, 5, 2, 1];
  var statusColors = [ok, accent2, warn, danger];
  var statusTotal = 31;

  var status = echarts.init(document.getElementById('chart-status'), null, { renderer: 'svg' });
  status.setOption({
    animation: false,
    grid: { left: 46, right: 108, top: 16, bottom: 12 },
    tooltip: Object.assign({}, tooltipBase, {
      trigger: 'item',
      formatter: function (p) {
        return p.name + '<br/>' + p.value + ' 台（' + (p.value / statusTotal * 100).toFixed(1) + '%）';
      }
    }),
    xAxis: { type: 'value', max: 26, show: false },
    yAxis: {
      type: 'category',
      data: statusNames,
      inverse: true,
      axisLine: { show: false },
      axisTick: { show: false },
      axisLabel: { color: 'rgba(206, 232, 248, 0.88)', fontSize: 12, fontFamily: CN, margin: 14 }
    },
    series: [
      {
        type: 'bar',
        barWidth: 13,
        itemStyle: {
          borderRadius: 6.5,
          color: function (p) {
            var c = statusColors[p.dataIndex];
            return new echarts.graphic.LinearGradient(0, 0, 1, 0, [
              { offset: 0, color: c + '22' },
              { offset: 1, color: c }
            ]);
          }
        },
        label: {
          show: true, position: 'right', distance: 10,
          fontSize: 11, fontFamily: NUM, color: 'rgba(214, 240, 255, 0.92)',
          formatter: function (p) {
            return p.value + ' 台 · ' + (p.value / statusTotal * 100).toFixed(1) + '%';
          }
        },
        data: statusValues
      }
    ]
  });
  charts.push(status);

  // ---------- 04 设备健康：7日渐变柱状 ----------
  var healthEl = document.getElementById('chart-health');
  var health = echarts.init(healthEl, null, { renderer: 'svg' });
  health.setOption({
    animation: false,
    grid: { left: 38, right: 16, top: 14, bottom: 22 },
    tooltip: Object.assign({}, tooltipBase, {
      trigger: 'axis',
      axisPointer: { type: 'shadow' },
      formatter: function (p) { return p[0].axisValue + '<br/>综合健康度 ' + p[0].data + '%'; }
    }),
    xAxis: {
      type: 'category',
      data: ['11-03', '11-04', '11-05', '11-06', '11-07', '11-08', '11-09'],
      axisLine: { lineStyle: { color: 'rgba(70, 160, 220, 0.22)' } },
      axisTick: { show: false },
      axisLabel: { color: muted, fontSize: 10.5, fontFamily: NUM }
    },
    yAxis: {
      type: 'value', min: 88, max: 96, splitNumber: 4,
      axisLine: { show: false },
      axisTick: { show: false },
      axisLabel: { color: muted, fontSize: 10, fontFamily: NUM, formatter: '{value}%' },
      splitLine: { lineStyle: { color: 'rgba(70, 160, 220, 0.10)', type: 'dashed' } }
    },
    series: [
      {
        type: 'bar',
        barWidth: '34%',
        itemStyle: {
          borderRadius: [3, 3, 0, 0],
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: accent },
            { offset: 1, color: 'rgba(38, 226, 255, 0.12)' }
          ]),
          shadowBlur: 10,
          shadowColor: 'rgba(38, 226, 255, 0.28)'
        },
        label: {
          show: true, position: 'top', distance: 4,
          color: 'rgba(200, 234, 255, 0.75)', fontSize: 9.5, fontFamily: NUM
        },
        data: [91.2, 91.8, 92.3, 92.0, 92.8, 93.1, 93.5],
        markLine: {
          silent: true,
          symbol: 'none',
          lineStyle: { color: 'rgba(255, 177, 59, 0.55)', type: 'dashed', width: 1 },
          label: { show: false },
          data: [{ yAxis: 93 }]
        }
      }
    ]
  });
  charts.push(health);

  // ---------- 05 设备维修效率：系统 × 阶段 堆叠柱 ----------
  var systems = ['粉碎系统', '配料称重', '发酵系统', '烘干系统', '包装码垛'];
  var phases = ['响应', '诊断', '维修', '验收'];
  var repairData = [
    [0.4, 0.8, 1.3, 0.3],
    [0.3, 1.0, 1.5, 0.4],
    [0.5, 0.7, 1.1, 0.3],
    [0.6, 0.9, 1.4, 0.3],
    [0.4, 0.6, 0.9, 0.2]
  ];
  var phaseColors = ['rgba(61, 123, 255, 0.85)', 'rgba(46, 160, 220, 0.9)', 'rgba(38, 226, 255, 0.9)', 'rgba(46, 230, 168, 0.85)'];
  var totals = repairData.map(function (r) { return r.reduce(function (a, b) { return a + b; }, 0); });

  var repair = echarts.init(document.getElementById('chart-repair'), null, { renderer: 'svg' });
  var repairSeries = phases.map(function (phase, pi) {
    return {
      name: phase,
      type: 'bar',
      stack: 'total',
      barWidth: 26,
      itemStyle: {
        color: phaseColors[pi],
        borderColor: 'rgba(4, 14, 24, 0.85)',
        borderWidth: 1.5
      },
      emphasis: { focus: 'series' },
      data: repairData.map(function (r) { return r[pi]; })
    };
  });
  repairSeries[3].label = {
    show: true, position: 'top', distance: 6,
    color: 'rgba(214, 240, 255, 0.92)', fontSize: 10.5, fontFamily: NUM,
    formatter: function (p) { return totals[p.dataIndex].toFixed(1) + 'h'; }
  };

  repair.setOption({
    animation: false,
    grid: { left: 40, right: 18, top: 34, bottom: 26 },
    tooltip: Object.assign({}, tooltipBase, {
      trigger: 'axis',
      axisPointer: { type: 'shadow' },
      formatter: function (ps) {
        var s = ps[0].axisValue + '<br/>';
        ps.forEach(function (p) { s += p.marker + p.seriesName + ' ' + p.data + ' h<br/>'; });
        s += '合计 ' + totals[ps[0].dataIndex].toFixed(1) + ' h';
        return s;
      }
    }),
    legend: {
      top: 0, right: 0, itemWidth: 8, itemHeight: 8, itemGap: 14, icon: 'roundRect',
      textStyle: { color: 'rgba(180, 210, 230, 0.85)', fontSize: 11, fontFamily: CN }
    },
    xAxis: {
      type: 'category',
      data: systems,
      axisLine: { lineStyle: { color: 'rgba(70, 160, 220, 0.22)' } },
      axisTick: { show: false },
      axisLabel: { color: 'rgba(190, 216, 234, 0.85)', fontSize: 11, fontFamily: CN }
    },
    yAxis: {
      type: 'value', max: 4, splitNumber: 4,
      axisLine: { show: false },
      axisTick: { show: false },
      axisLabel: { color: muted, fontSize: 10, fontFamily: NUM, formatter: '{value}h' },
      splitLine: { lineStyle: { color: 'rgba(70, 160, 220, 0.10)', type: 'dashed' } }
    },
    series: repairSeries
  });
  charts.push(repair);

  window.addEventListener('resize', function () {
    fitStage();
    charts.forEach(function (c) { c.resize(); });
  });
})();
