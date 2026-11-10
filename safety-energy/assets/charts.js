// 安环能耗大屏图表
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

  // ---------- 舞台自适应 ----------
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

  // ================= 左侧 01：近7日用电量 =================
  var energy = echarts.init(document.getElementById('chart-energy'), null, { renderer: 'svg' });
  energy.setOption({
    animation: false,
    grid: { left: 46, right: 22, top: 22, bottom: 22 },
    tooltip: Object.assign({}, tooltipBase, {
      trigger: 'axis',
      formatter: function (p) { return p[0].axisValue + '<br/>用电量 ' + p[0].data.toLocaleString() + ' kWh'; }
    }),
    xAxis: {
      type: 'category',
      boundaryGap: false,
      data: ['11-03', '11-04', '11-05', '11-06', '11-07', '11-08', '11-09'],
      axisLine: { lineStyle: { color: 'rgba(70, 160, 220, 0.22)' } },
      axisTick: { show: false },
      axisLabel: { color: muted, fontSize: 10.5, fontFamily: NUM }
    },
    yAxis: {
      type: 'value', min: 14000, max: 20000, splitNumber: 4,
      axisLine: { show: false },
      axisTick: { show: false },
      axisLabel: {
        color: muted, fontSize: 9.5, fontFamily: NUM,
        formatter: function (v) { return v / 1000 + 'k'; }
      },
      splitLine: { lineStyle: { color: 'rgba(70, 160, 220, 0.10)', type: 'dashed' } }
    },
    series: [
      {
        type: 'line',
        smooth: true,
        symbol: 'circle',
        symbolSize: 4.5,
        showSymbol: false,
        lineStyle: { width: 2, color: accent, shadowBlur: 12, shadowColor: 'rgba(38, 226, 255, 0.45)' },
        itemStyle: { color: accent, borderColor: 'rgba(4, 14, 24, 0.9)', borderWidth: 1 },
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(38, 226, 255, 0.26)' },
            { offset: 1, color: 'rgba(38, 226, 255, 0.01)' }
          ])
        },
        data: [16200, 15800, 17100, 16900, 17600, 17400, 18640],
        markPoint: {
          symbol: 'circle', symbolSize: 7,
          itemStyle: { color: accent, shadowBlur: 14, shadowColor: accent },
          label: {
            show: true, position: 'top', distance: 8,
            color: accent, fontSize: 10.5, fontFamily: NUM,
            formatter: '18,640'
          },
          data: [{ coord: ['11-09', 18640] }]
        }
      }
    ]
  });
  charts.push(energy);

  // ================= 左侧 02：厂界噪声 =================
  var noisePoints = ['N1 东厂界', 'N2 南厂界', 'N3 西厂界', 'N4 北厂界', 'N5 敏感点'];
  var noiseDay = [54, 56, 57, 56, 56];
  var noiseNight = [43, 45, 47, 47, 45];

  var noise = echarts.init(document.getElementById('chart-noise'), null, { renderer: 'svg' });
  noise.setOption({
    animation: false,
    grid: { left: 40, right: 20, top: 30, bottom: 40 },
    tooltip: Object.assign({}, tooltipBase, { trigger: 'axis', axisPointer: { type: 'shadow' } }),
    legend: {
      top: 0, right: 0, itemWidth: 8, itemHeight: 8, itemGap: 14, icon: 'roundRect',
      textStyle: { color: 'rgba(180, 210, 230, 0.85)', fontSize: 11, fontFamily: CN }
    },
    xAxis: {
      type: 'category',
      data: noisePoints,
      axisLine: { lineStyle: { color: 'rgba(70, 160, 220, 0.22)' } },
      axisTick: { show: false },
      axisLabel: { color: 'rgba(186, 214, 234, 0.85)', fontSize: 10, fontFamily: CN, interval: 0, margin: 10 }
    },
    yAxis: {
      type: 'value', min: 30, max: 65, splitNumber: 3,
      axisLine: { show: false },
      axisTick: { show: false },
      axisLabel: { color: muted, fontSize: 9.5, fontFamily: NUM, formatter: '{value}' },
      splitLine: { lineStyle: { color: 'rgba(70, 160, 220, 0.10)', type: 'dashed' } }
    },
    series: [
      {
        name: '昼间',
        type: 'bar',
        barWidth: 11,
        itemStyle: {
          borderRadius: [3, 3, 0, 0],
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: accent },
            { offset: 1, color: 'rgba(38, 226, 255, 0.15)' }
          ])
        },
        label: { show: true, position: 'top', color: 'rgba(200, 234, 255, 0.75)', fontSize: 9.5, fontFamily: NUM },
        data: noiseDay
      },
      {
        name: '夜间',
        type: 'bar',
        barWidth: 11,
        itemStyle: {
          borderRadius: [3, 3, 0, 0],
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(61, 123, 255, 0.9)' },
            { offset: 1, color: 'rgba(61, 123, 255, 0.12)' }
          ])
        },
        label: { show: true, position: 'top', color: 'rgba(180, 205, 235, 0.7)', fontSize: 9.5, fontFamily: NUM },
        data: noiseNight,
        markLine: {
          silent: true,
          symbol: 'none',
          lineStyle: { color: 'rgba(255, 177, 59, 0.55)', type: 'dashed', width: 1 },
          label: { show: false },
          data: [{ yAxis: 60 }, { yAxis: 50 }]
        }
      }
    ]
  });
  charts.push(noise);

  // ================= 左侧 03：空气质量占标率 =================
  var airItems = ['O₃', 'PM2.5', 'PM10', 'NO₂', 'CO', 'SO₂'];
  var airRate = [72.5, 60.0, 55.7, 30.0, 25.0, 16.7];
  var airValue = ['116 μg/m³', '21 μg/m³', '39 μg/m³', '12 μg/m³', '1 mg/m³', '10 μg/m³'];
  var airStd = ['160 μg/m³', '35 μg/m³', '70 μg/m³', '40 μg/m³', '4 mg/m³', '60 μg/m³'];

  var air = echarts.init(document.getElementById('chart-air'), null, { renderer: 'svg' });
  air.setOption({
    animation: false,
    grid: { left: 52, right: 56, top: 8, bottom: 6 },
    tooltip: Object.assign({}, tooltipBase, {
      trigger: 'item',
      formatter: function (p) {
        var i = p.dataIndex;
        return airItems[i] + '<br/>现状 ' + airValue[i] + '<br/>标准 ' + airStd[i] + '<br/>占标率 ' + p.value + '%';
      }
    }),
    xAxis: { type: 'value', max: 100, show: false },
    yAxis: {
      type: 'category',
      data: airItems,
      inverse: true,
      axisLine: { show: false },
      axisTick: { show: false },
      axisLabel: { color: 'rgba(206, 232, 248, 0.88)', fontSize: 11.5, fontFamily: CN, margin: 12 }
    },
    series: [
      {
        type: 'bar',
        barWidth: 9,
        itemStyle: {
          borderRadius: 4.5,
          color: function (p) {
            var gradient = new echarts.graphic.LinearGradient(0, 0, 1, 0, [
              { offset: 0, color: 'rgba(38, 226, 255, 0.18)' },
              { offset: 1, color: p.value >= 60 ? warn : accent }
            ]);
            return gradient;
          }
        },
        label: {
          show: true, position: 'right', distance: 8,
          color: 'rgba(214, 240, 255, 0.92)', fontSize: 10.5, fontFamily: NUM,
          formatter: '{c}%'
        },
        showBackground: true,
        backgroundStyle: { color: 'rgba(120, 170, 210, 0.08)', borderRadius: 4.5 },
        data: airRate
      }
    ]
  });
  charts.push(air);

  // ================= 右侧：工段导航 + 统计面板 =================
  var periods = ['00-04', '04-08', '08-12', '12-16', '16-20', '20-24'];

  var sections = [
    {
      name: '全厂总览', tag: '全厂总览',
      stats: { power: '18,640', water: '126', gas: '3,240', dust: '12.6', noise: '57', carbon: '12,480' },
      now: [2100, 2800, 3400, 3200, 3800, 3340],
      base: [2200, 2900, 3300, 3100, 3600, 3200],
      note: '统计口径：本月累计 · 5 大工段'
    },
    {
      name: '原料与前处理段', tag: '原料与前处理段',
      stats: { power: '4,280', water: '18', gas: '0', dust: '3.4', noise: '56', carbon: '2,760' },
      now: [480, 640, 780, 720, 880, 780],
      base: [500, 660, 760, 700, 840, 760],
      note: '含投料口 2 套集尘罩 + 2 套脉冲布袋除尘器'
    },
    {
      name: '一混配料与发酵段', tag: '一混配料与发酵段',
      stats: { power: '5,120', water: '32', gas: '0', dust: '2.8', noise: '58', carbon: '3,350' },
      now: [560, 760, 940, 880, 1060, 920],
      base: [540, 740, 900, 860, 1000, 900],
      note: '含配料仓封闭 + 1 套脉冲布袋除尘器'
    },
    {
      name: '二混配料与混合段', tag: '二混配料与混合段',
      stats: { power: '3,460', water: '21', gas: '0', dust: '2.2', noise: '57', carbon: '2,240' },
      now: [380, 520, 640, 600, 720, 600],
      base: [400, 540, 620, 580, 700, 590],
      note: '混合工序粉尘经密闭管道收集后回用'
    },
    {
      name: '烘干包装码垛段', tag: '烘干包装码垛段',
      stats: { power: '4,260', water: '14', gas: '3,240', dust: '2.9', noise: '55', carbon: '3,120' },
      now: [430, 600, 780, 720, 940, 790],
      base: [460, 620, 760, 710, 900, 780],
      note: '天然气烘干为主，厌氧发酵异味经通风稀释'
    },
    {
      name: '动力控制环保段', tag: '动力控制环保段',
      stats: { power: '1,520', water: '41', gas: '0', dust: '1.3', noise: '59', carbon: '1,010' },
      now: [180, 230, 280, 270, 320, 240],
      base: [200, 240, 290, 280, 330, 250],
      note: '空压、除尘风机与环保设施集中控制'
    }
  ];

  var nav = document.getElementById('sideNav');
  var statsEl = document.getElementById('stats');
  var tagEl = document.getElementById('panelTag');
  var footEl = document.getElementById('panelFootLeft');

  sections.forEach(function (s, i) {
    var btn = document.createElement('button');
    btn.className = 'nav-item' + (i === 0 ? ' active' : '');
    btn.innerHTML = '<span class="nav-idx">' + String(i + 1).padStart(2, '0') + '</span>' + s.name;
    btn.addEventListener('click', function () {
      var items = nav.querySelectorAll('.nav-item');
      for (var k = 0; k < items.length; k++) items[k].classList.remove('active');
      btn.classList.add('active');
      renderSection(i);
    });
    nav.appendChild(btn);
  });

  var sectionChart = echarts.init(document.getElementById('chart-section'), null, { renderer: 'svg' });
  charts.push(sectionChart);

  function renderSection(index) {
    var s = sections[index];
    tagEl.textContent = s.tag;
    footEl.textContent = s.note;

    var cells = [
      { k: '用电量', v: s.stats.power, u: 'kWh' },
      { k: '用水量', v: s.stats.water, u: 't' },
      { k: '天然气', v: s.stats.gas, u: 'm³' },
      { k: '粉尘排放', v: s.stats.dust, u: 'kg' },
      { k: '厂界噪声', v: s.stats.noise, u: 'dB(A)' },
      { k: '碳排放', v: s.stats.carbon, u: 'kgCO₂' }
    ];
    statsEl.innerHTML = cells.map(function (c) {
      return '<div class="stat"><div class="k">' + c.k + '</div>' +
        '<div class="v">' + c.v + '<small>' + c.u + '</small></div></div>';
    }).join('');

    sectionChart.setOption({
      animation: false,
      grid: { left: 52, right: 28, top: 34, bottom: 26 },
      tooltip: Object.assign({}, tooltipBase, {
        trigger: 'axis',
        axisPointer: { type: 'shadow' },
        formatter: function (ps) {
          var str = ps[0].axisValue + ' 时段<br/>';
          ps.forEach(function (p) { str += p.marker + p.seriesName + ' ' + p.data.toLocaleString() + ' kWh<br/>'; });
          return str;
        }
      }),
      legend: {
        top: 0, right: 0, itemWidth: 8, itemHeight: 8, itemGap: 16, icon: 'roundRect',
        textStyle: { color: 'rgba(180, 210, 230, 0.85)', fontSize: 11, fontFamily: CN }
      },
      xAxis: {
        type: 'category',
        data: periods,
        axisLine: { lineStyle: { color: 'rgba(70, 160, 220, 0.22)' } },
        axisTick: { show: false },
        axisLabel: { color: 'rgba(186, 214, 234, 0.85)', fontSize: 11, fontFamily: NUM }
      },
      yAxis: {
        type: 'value',
        axisLine: { show: false },
        axisTick: { show: false },
        axisLabel: {
          color: muted, fontSize: 10, fontFamily: NUM,
          formatter: function (v) { return v >= 1000 ? (v / 1000) + 'k' : v; }
        },
        splitLine: { lineStyle: { color: 'rgba(70, 160, 220, 0.10)', type: 'dashed' } }
      },
      series: [
        {
          name: '本月用电',
          type: 'bar',
          barWidth: '26%',
          itemStyle: {
            borderRadius: [3, 3, 0, 0],
            color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
              { offset: 0, color: accent },
              { offset: 1, color: 'rgba(38, 226, 255, 0.12)' }
            ]),
            shadowBlur: 10,
            shadowColor: 'rgba(38, 226, 255, 0.25)'
          },
          label: {
            show: true, position: 'top', distance: 5,
            color: 'rgba(206, 238, 255, 0.9)', fontSize: 10, fontFamily: NUM
          },
          data: s.now
        },
        {
          name: '上月基准',
          type: 'bar',
          barWidth: '26%',
          itemStyle: {
            borderRadius: [3, 3, 0, 0],
            color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
              { offset: 0, color: 'rgba(61, 123, 255, 0.65)' },
              { offset: 1, color: 'rgba(61, 123, 255, 0.08)' }
            ])
          },
          data: s.base
        }
      ]
    });
  }

  renderSection(0);

  window.addEventListener('resize', function () {
    fitStage();
    charts.forEach(function (c) { c.resize(); });
  });
})();
