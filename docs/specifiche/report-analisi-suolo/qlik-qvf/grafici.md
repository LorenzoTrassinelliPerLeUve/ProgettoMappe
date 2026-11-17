
## 0 - FILITRI
- **table** dims=["RagioneSociale (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "DATA PRELIEVO (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - {"qInterColumnSortOrder": [0, 1]}

## Acidità di Scambio % CSC
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=NomeVigneto (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Acidità di Scambio % CSC = Avg([Acidità di Scambio  % su CSC]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "min", "refLineExpr": {"value": 2, "label": "2"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "max", "refLineExpr": {"value": 15, "label": "15"}, "paletteColor": {"index": -1, "color": "#b82d23"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Acidità di Scambio % CSC", "key": "Avg([Acidità di Scambio  % su CSC])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "minMax", "min": 0, "max": 15}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: Acidità di Scambio % CSC = Avg([Acidità di Scambio  % su CSC]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Azoto (1)
- **combochart** dims=["=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Azoto (N) ‰ = Avg([Azoto totale (N) ‰]) [F:#.##0,0]
  - misura:  = Avg(MinN) []
  - misura:  = Avg(MaxN) []
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true, "show": false, "showLineLabels": false, "showBarLabels": false, "showBarSegmentLabels": false, "showBarTotalLabels": true, "showMarkerLabels": false}, "refLine": [], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Azoto (N) ‰", "key": "Avg([Azoto totale (N) ‰])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"min": 0, "max": 4}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 3, 2, 1]}
- **kpi** dims=[]
  - misura: Azoto N-Totale  (‰) = Avg([Azoto totale (N) ‰]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Azoto
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Azoto (N) ‰ = Avg([Azoto totale (N) ‰]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Azoto (N) ‰", "key": "Avg([Azoto totale (N) ‰])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": true, "minMax": "max", "min": 0, "max": 4}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: Azoto N-Totale  (‰) = Avg([Azoto totale (N) ‰]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Boro (B)
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura:  = Avg([Boro assimilabile (B) ppm]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "min", "refLineExpr": {"value": 1, "label": "1"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "max", "refLineExpr": {"value": 20, "label": "20"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": ""}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "='Boro (B) assimilabile ppm'", "key": "Avg([Boro assimilabile (B) ppm])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 0, "max": 2.5}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura:  = Avg([Boro assimilabile (B) ppm]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## CSC
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: CSC (T)  cmol  (₊)/kg = Avg([Capacità di Sc. Cation. (CSC) meq/100g]) [F:#.##0,00]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "min", "refLineExpr": {"value": 5, "label": "5"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "med", "refLineExpr": {"value": 15, "label": "15"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "max", "refLineExpr": {"value": 25, "label": "25"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "CSC (T)  cmol  (₊)/kg", "key": "Avg([Capacità di Sc. Cation. (CSC) meq/100g])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 0, "max": 30}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: CSC (T)cmol  (₊)/kg = Avg([Capacità di Sc. Cation. (CSC) meq/100g]) [F:#.##0,00]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## C_N
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: C/N = Avg([Rapporto C/N]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "", "refLineExpr": {"value": 10, "label": "10"}, "paletteColor": {"index": -1, "color": "#b82d23"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "C/N", "key": "Avg([Rapporto C/N])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "minMax", "min": 0, "max": 15}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: C/N = Avg([Rapporto C/N]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Ca % CSC
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Ca % CSC = Avg([Calcio scambiabile (Ca) % su CSC]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "min", "refLineExpr": {"value": 70, "label": "70"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "max", "refLineExpr": {"value": 85, "label": "85"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Ca % CSC", "key": "Avg([Calcio scambiabile (Ca) % su CSC])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": true, "minMax": "minMax", "min": 0, "max": 100}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: Ca Scambiabile [% CSC (T)] = Avg([Calcio scambiabile (Ca) % su CSC]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Ca Scambiabile ppm
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Ca Scambiabile ppm = Avg([Calcio scambiabile (Ca) ppm]) [F:#.##0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Ca Scambiabile ppm", "key": "Avg([Calcio scambiabile (Ca) ppm])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "min", "min": 0, "max": 100}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: Ca Scambiabile ppm = Avg([Calcio scambiabile (Ca) ppm]) [F:#.##0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Ca_Mg
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Rapporto Ca/Mg = Avg([Rapporto Ca/Mg]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "min", "refLineExpr": {"value": 8.5, "label": "8.5"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "max", "refLineExpr": {"value": 10, "label": "10"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Rapporto Ca/Mg", "key": "Avg([Rapporto Ca/Mg])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 0, "max": 15}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: Rapporto Ca/Mg = Avg([Rapporto Ca/Mg]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Calcare Attivo
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Calcare Attivo (CaCo3)  ‰ = Avg([Calcare attivo (CaCO3)  %])*10 [F:#.##0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Calcare Attivo (CaCo3)  ‰", "key": "Avg([Calcare attivo (CaCO3)  %])*10", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 6, "max": 400}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: Calcare Attivo - CaCO₃ (‰) = Avg([Calcare attivo (CaCO3)  %])*10 [F:#.##0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Calcio idrosolubile (Ca) ppm
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura:  = Avg([Calcio idrosolubile (Ca) ppm]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "min", "refLineExpr": {"value": 50, "label": "50"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "max", "refLineExpr": {"value": 100, "label": "100"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": ""}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "='Calcio (Ca) idrosolubile (ppm)'", "key": "Avg([Calcio idrosolubile (Ca) ppm])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": true, "minMax": "max", "min": 0, "max": 2.5}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura:  = Avg([Calcio idrosolubile (Ca) ppm]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Carbonati Totali
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "CODICE PUNTO (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Carbonati Totali (CaCo3)  ‰ = Avg([Carbonati totali (CaCO3)  %])*10 [F:#.##0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Carbonati Totali (CaCo3)  ‰", "key": "Avg([Carbonati totali (CaCO3)  %])*10", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 6, "max": 800}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 2, 1]}
- **kpi** dims=[]
  - misura: Carbonati Totali - CaCO₃ (‰) = Avg([Carbonati totali (CaCO3)  %])*10 [F:#.##0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Cloro idrosolubile (Cl) ppm
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura:  = Avg([Cloro idrosolubile (Cl) ppm]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "min", "refLineExpr": {"value": 30, "label": "30"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "max", "refLineExpr": {"value": 50, "label": "50"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": ""}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "='Cloro (Cl) idrosolubile (ppm)'", "key": "Avg([Cloro idrosolubile (Cl) ppm])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 0, "max": 2.5}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura:  = Avg([Cloro idrosolubile (Cl) ppm]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Coef K2
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Coef. K2 = Avg([K2 coeff mineralizzazione]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "max", "refLineExpr": {"value": 3, "label": "3"}, "paletteColor": {"index": -1, "color": "#b82d23"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "min", "refLineExpr": {"value": 1, "label": "1"}, "paletteColor": {"index": -1, "color": "#b82d23"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Coef. K2", "key": "Avg([K2 coeff mineralizzazione])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 0, "max": 4}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: Coefficiente K2 = Avg([K2 coeff mineralizzazione]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Composizione CSC
- **text-image** ''
- **combochart** dims=["=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Ca cmol  (₊)/kg. = avg([Calcio scambiabile (Ca) ppm])/200.4 [F:#.##0,00]
  - misura: Mg cmol  (₊)/kg = avg([Magnesio scambiabile (Mg) ppm])/121.5 []
  - misura: K cmol  (₊)/kg = avg([Potassio scambiabile (K) ppm])/391 [F:#.##0,00]
  - misura: Na cmol  (₊)/kg = avg([Sodio scambiabile (Na) ppm])/229.9 []
  - misura: Acidità di Scambio cmol  (₊)/kg = avg([Acidità di scambio meq]) [F:#.##0,00]
  - {"orientation": "vertical", "barGrouping": {"grouping": "stacked"}, "dataPoint": {"show": false, "showLineLabels": false, "showBarLabels": true, "showBarSegmentLabels": false, "showBarTotalLabels": true, "showMarkerLabels": false, "showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [], "color": {"auto": true, "mode": "primary", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Avg([Capacità di Sc. Cation. (CSC) meq/100g])", "key": "Avg([Capacità di Sc. Cation. (CSC) meq/100g])", "type": "expression"}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "auto", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"min": 0, "max": 30}, "legend": {"show": true, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [1, 2, 5, 0]}
- **kpi** dims=[]
  - misura: Ca cmol  (₊)/kg = Avg([Calcio scambiabile (Ca) meq]) [F:#.##0,00]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **kpi** dims=[]
  - misura: Mg cmol  (₊)/kg = Avg([Magnesio scambiabile (Mg) meq]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **kpi** dims=[]
  - misura: K cmol  (₊)/kg = Avg([Potassio scambiabile (K) meq]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **kpi** dims=[]
  - misura: Na cmol  (₊)/kg = Avg([Sodio scambiabile (Na) meq]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **kpi** dims=[]
  - misura: Acidità cmol  (₊)/kg = Avg([Acidità di scambio meq]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}

## Conducibilità
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Conducibilità mS/cm = Avg([Conducibilità mS/cm]) [F:#.##0,00]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Conducibilità mS/cm", "key": "Avg([Conducibilità mS/cm])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": true, "minMax": "minMax", "min": 0, "max": 5}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: Conducibilità (mS/cm) = Avg([Conducibilità mS/cm]) [F:#.##0,00]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Considerazioni (1) (1)
- **text-image** ''

## Considerazioni (1)
- **text-image** ''

## Considerazioni
- **text-image** ''

## Copertina
- **text-image** ''
- **text-image** '![ZVmuLac](/api/v1/apps/bc52320c\\-8cdd\\-4e26\\-a374\\-2c0958aad934/media/files/foto%20campionamento.png)'
- **text-image** '![qKRmxgs](/api/v1/apps/bc52320c\\-8cdd\\-4e26\\-a374\\-2c0958aad934/media/files/triangolo%20con%20terreno%20USDA.jpg)'

## Effetti sul vino
- **text-image** ''
- **text-image** '%[Terrenicalcareo\\-marnosi : I terrenicalcareo\\-marnosi sono terreni composti dacalcare contenente una certa quantità di argilla, la ~@[marna](https://www.quattrocalici.it/glossario\\-vino/suoli\\-marno'

## Ferro (Fe) DTPA
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura:  = Avg([Ferro assimilabile (Fe) ppm DTPA]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "", "refLineExpr": {"value": 2.5, "label": "2.5"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "", "refLineExpr": {"value": 4.5, "label": "4.5"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "='Ferro(Fe) assimilabile (DTPA) ppm'", "key": "Avg([Ferro assimilabile (Fe) ppm DTPA])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 0, "max": 10}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura:  = Avg([Ferro assimilabile (Fe) ppm DTPA]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Ferro (Fe) EDTA
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=NomeVigneto (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura:  = Avg([Ferro assimilabile (Fe) ppm EDTA]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "min", "refLineExpr": {"value": 45, "label": "45"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "med", "refLineExpr": {"value": 166, "label": "166"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "max", "refLineExpr": {"value": 350, "label": "350"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": ""}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "='Ferro(Fe) assimilabile (ETPA) ppm'", "key": "Avg([Ferro assimilabile (Fe) ppm EDTA])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "min", "min": 0, "max": 15}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura:  = Avg([Ferro assimilabile (Fe) ppm EDTA]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## K % CSC
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: K % CSC = Avg([Potassio scambiabile (K) % su CSC]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "min,", "refLineExpr": {"value": 2, "label": "2"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "max", "refLineExpr": {"value": 5, "label": "5"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "K % CSC", "key": "Avg([Potassio scambiabile (K) % su CSC])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 0, "max": 9}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: K Scambiabile [% CSC (T)] = Avg([Potassio scambiabile (K) % su CSC]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## K Scambiabile ppm
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: K Scambiabile ppm = Avg([Potassio scambiabile (K) ppm]) [F:#.##0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "K Scambiabile ppm", "key": "Avg([Potassio scambiabile (K) ppm])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "min", "min": 0, "max": 100}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: K Scambiabile (ppm) = Avg([Potassio scambiabile (K) ppm]) [F:#.##0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## MG % CSC
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Mg % CSC = Avg([Magnesio scambiabile (Mg) % su CSC]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "min", "refLineExpr": {"value": 7, "label": "7"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "max", "refLineExpr": {"value": 10, "label": "10"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Mg % CSC", "key": "Avg([Magnesio scambiabile (Mg) % su CSC])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": true, "minMax": "min", "min": 0, "max": 7}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: Mg Scambiabile [% CSC (T)] = Avg([Magnesio scambiabile (Mg) % su CSC]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## MG Scambiabile ppm
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Mg Scambiabile ppm = Avg([Magnesio scambiabile (Mg) ppm]) [F:#.##0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Mg Scambiabile ppm", "key": "Avg([Magnesio scambiabile (Mg) ppm])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "min", "min": 0, "max": 100}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: Mg Scambiabile (ppm) = Avg([Magnesio scambiabile (Mg) ppm]) [F:#.##0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Magnesio idrosolubile (Mg) ppm
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura:  = Avg([Magnesio idrosolubile (Mg)]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "min", "refLineExpr": {"value": 4, "label": "4"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "max", "refLineExpr": {"value": 7, "label": "7"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "='Magnesio (Mg) idrosolubile (ppm)'", "key": "Avg([Magnesio idrosolubile (Mg)])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 0, "max": 10}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura:  = Avg([Magnesio idrosolubile (Mg)]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Manganese (Mn) DTPA
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura:  = Avg([Manganese assimilabile (Mn) ppm DTPA]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "", "refLineExpr": {"value": 1, "label": "1"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "", "refLineExpr": {"value": 1.5, "label": "1.5"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "='Manganese (Mn) assimilabile (DTPA) ppm'", "key": "Avg([Manganese assimilabile (Mn) ppm DTPA])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 0, "max": 5}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura:  = Avg([Manganese assimilabile (Mn) ppm DTPA]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Manganese (Mn) EDTA
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=NomeVigneto (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura:  = Avg([Manganese assimilabile (Mn) ppm EDTA]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "min", "refLineExpr": {"value": 100, "label": "100"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "med", "refLineExpr": {"value": 260, "label": "260"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "max", "refLineExpr": {"value": 350, "label": "350"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": ""}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "='Manganese (Mn) assimilabile (ETPA) ppm'", "key": "Avg([Manganese assimilabile (Mn) ppm EDTA])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "min", "min": 0, "max": 15}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura:  = Avg([Manganese assimilabile (Mn) ppm EDTA]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Mg_K
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Rapporto Mg/K = Avg([Rapporto Mg/K]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "min", "refLineExpr": {"value": 2, "label": "2"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "max", "refLineExpr": {"value": 3, "label": "3"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Rapporto Mg/K", "key": "Avg([Rapporto Mg/K])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 1, "autoMinMax": false, "minMax": "max", "min": 0, "max": 7}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: Rapporto Mg/K = Avg([Rapporto Mg/K]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Molibdeno (Mo)
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=NomeVigneto (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura:  = Avg([Molibdeno  assimilabile (Mo) ppm]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "", "refLineExpr": {"value": 0.2, "label": "0.2"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "='Boro (B) assimilabile ppm'", "key": "Avg([Molibdeno  assimilabile (Mo) ppm])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 0, "max": 2.5}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura:  = Avg([Molibdeno  assimilabile (Mo) ppm]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Na % CSC
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Na % CSC = Avg([Sodio scambiabile (Na) % su CSC]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "min", "refLineExpr": {"value": 2, "label": "2"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "max", "refLineExpr": {"value": 15, "label": "15"}, "paletteColor": {"index": -1, "color": "#b82d23"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Na % CSC", "key": "Avg([Sodio scambiabile (Na) % su CSC])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": true, "minMax": "max", "min": 0, "max": 18}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: Na Scambiabile [% CSC (T)]CSC = Avg([Sodio scambiabile (Na) % su CSC]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Na Scambiabile ppm
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Na Scambiabile ppm = Avg([Sodio scambiabile (Na) ppm]) [F:#.##0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Na Scambiabile ppm", "key": "Avg([Sodio scambiabile (Na) ppm])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "min", "min": 0, "max": 100}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: Na Scambiabile (ppm) = Avg([Sodio scambiabile (Na) ppm]) [F:#.##0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## P Bray Kurz (1)
- **combochart** dims=["=NomeVigneto (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Fosforo (P) assimilabile (Bray Kurz) ppm = Avg([Fosforo assimilabile (P) Bray Kurz ppm]) [F:#.##0,0]
  - misura: Limite Min = if(Avg([Capacità di Sc. Cation. (CSC) meq/100g])>0,25) []
  - misura: Limite Max = if(Avg([Capacità di Sc. Cation. (CSC) meq/100g])>0,37) []
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true, "show": false, "showLineLabels": false, "showBarLabels": false, "showBarSegmentLabels": false, "showBarTotalLabels": true, "showMarkerLabels": false}, "refLine": [], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Fosforo (P) assimilabile (Bray Kurz) ppm", "key": "Avg([Fosforo assimilabile (P) Bray Kurz ppm])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"min": 0, "max": 35}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2, 3]}
- **kpi** dims=[]
  - misura: Fosforo (P) assimilabile (Bray Kurz) ppm = Avg([Fosforo assimilabile (P) Bray Kurz ppm]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## P Bray Kurz
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=NomeVigneto (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Fosforo (P) assimilabile (Bray Kurz) ppm = Avg([Fosforo assimilabile (P) Bray Kurz ppm]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Fosforo (P) assimilabile (Bray Kurz) ppm", "key": "Avg([Fosforo assimilabile (P) Bray Kurz ppm])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 0, "max": 35}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: Fosforo (P) assimilabile (Bray Kurz) ppm = Avg([Fosforo assimilabile (P) Bray Kurz ppm]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## P Olsen (1)
- **combochart** dims=["=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Fosforo (P) assimilabile (Olsen) ppm = Avg([Fosforo assimilabile (P) Olsen ppm]) [F:#.##0,0]
  - misura: Limite Min = avg(MinP)
 []
  - misura: Limite Max = avg(MaxP) []
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true, "show": false, "showLineLabels": false, "showBarLabels": false, "showBarSegmentLabels": false, "showBarTotalLabels": true, "showMarkerLabels": false}, "refLine": [], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Fosforo (P) assimilabile (Olsen) ppm", "key": "Avg([Fosforo assimilabile (P) Olsen ppm])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"min": 0, "max": 35}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2, 3]}
- **kpi** dims=[]
  - misura: Fosforo (P) assimilabile (Olsen) ppm = Avg([Fosforo assimilabile (P) Olsen ppm]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## P Olsen
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Fosforo (P) assimilabile (Olsen) ppm = Avg([Fosforo assimilabile (P) Olsen ppm]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Fosforo (P) assimilabile (Olsen) ppm", "key": "Avg([Fosforo assimilabile (P) Olsen ppm])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": true, "minMax": "max", "min": 0, "max": 35}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: Fosforo (P) assimilabile (Olsen) ppm = Avg([Fosforo assimilabile (P) Olsen ppm]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Parametri Idropedologici
- **text-image** ''
- **kpi** dims=[]
  - misura:  = Avg(PuntoDiAppassimentoPermanente) []
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **kpi** dims=[]
  - misura: Acqua Disponibile = Avg(AcquaDisponibile_mm_m) []
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **kpi** dims=[]
  - misura: Capacità di Campo = Avg(CapacitaDiCampo) [F:#.##0,00]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **table** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "NomeVigneto (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "Descrizione (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "CODICE PUNTO (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura:  = Avg(CapacitaDiCampo) [F:#.##0,00]
  - misura:  = Avg(PuntoDiAppassimentoPermanente) [F:#.##0,00]
  - misura:  = Avg(AcquaDisponibile_mm_m) [F:#.##0,00]
  - misura: Peso Specifico (t/m³) = Avg(PesoSpecificoApparente) [F:#.##0,00]
  - misura: Conducibilità Idraulica (cm/h) = Avg(ConducibilitaIdraulicaSaturazione) [F:#.##0,00]
  - {"qInterColumnSortOrder": [0, 1, 3, 2, 8, 4, 5, 6, 7]}
- **kpi** dims=[]
  - misura: Conducibilità Idraulica = Avg(ConducibilitaIdraulicaSaturazione) []
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **kpi** dims=[]
  - misura: Peso Specifico = Avg(PesoSpecificoApparente) []
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}

## Potassio idrosolubile (K) ppm
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura:  = Avg([Potassio idrosolubile (K) ppm]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "min", "refLineExpr": {"value": 8, "label": "8"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "max", "refLineExpr": {"value": 15, "label": "15"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "='Potassio (K) idrosolubile (ppm)'", "key": "Avg([Potassio idrosolubile (K) ppm])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 0, "max": 20}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura:  = Avg([Potassio idrosolubile (K) ppm]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Rame (Cu) DTPA
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Rame (Cu) assimilabile (DTPA) ppm = Avg([Rame assimilabile (Cu) ppm DTPA]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "", "refLineExpr": {"value": 0.2, "label": "0.2"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "", "refLineExpr": {"value": 1, "label": "1"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Rame (Cu) assimilabile (DTPA) ppm", "key": "Avg([Rame assimilabile (Cu) ppm DTPA])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 0, "max": 5}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura:  = Avg([Rame assimilabile (Cu) ppm DTPA]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Rame (Cu) ETDA
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=NomeVigneto (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura:  = Avg([Rame assimilabile (Cu) ppm EDTA]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "min", "refLineExpr": {"value": 1.5, "label": "1.5"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "med", "refLineExpr": {"value": 6, "label": "6"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "max", "refLineExpr": {"value": 12, "label": "12"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "='Rame (Cu) assimilabile (ETPA) ppm'", "key": "Avg([Rame assimilabile (Cu) ppm EDTA])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 0, "max": 15}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura:  = Avg([Rame assimilabile (Cu) ppm EDTA]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Saturazione Basica
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Saturazione Basica (%) = Avg([Saturazione basica %]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "", "refLineExpr": {"value": 85, "label": "85"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Saturazione Basica (%)", "key": "Avg([Saturazione basica %])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "minMax", "min": 0, "max": 100}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: Saturazione Basica (%) = Avg([Saturazione basica %]) [F:#.##0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Sodio idrosolubile (Na) ppm
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura:  = Avg([Sodio idrosolubile (Na) ppm]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "min", "refLineExpr": {"value": 15, "label": "15"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "max", "refLineExpr": {"value": 20, "label": "20"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "='Sodio (Na) idrosolubile (ppm)'", "key": "Avg([Sodio idrosolubile (Na) ppm])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 0, "max": 22}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 2, 1]}
- **kpi** dims=[]
  - misura:  = Avg([Sodio idrosolubile (Na) ppm]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Sostanza Organica (1)
- **combochart** dims=["=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Sostanza Organica  ‰ = Avg([Sostanza organica %])*10 [F:#.##0,0]
  - misura:  = Avg(MinSO) []
  - misura:  = Avg(MaxSO) []
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true, "show": false, "showLineLabels": false, "showBarLabels": true, "showBarSegmentLabels": false, "showBarTotalLabels": true, "showMarkerLabels": false}, "refLine": [], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Sostanza Organica  ‰", "key": "Avg([Sostanza organica %])*10", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"min": 0, "max": 400}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [3, 2, 1, 0]}
- **kpi** dims=[]
  - misura: Sostanza Organica  (‰) = Avg([Sostanza organica %])*10 [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Sostanza Organica su Argilla
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Sostanza Organica su Argilla % = Avg([SO/Argilla %]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Sostanza Organica su Argilla %", "key": "Avg([SO/Argilla %])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 0, "max": 10}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 2, 1]}
- **kpi** dims=[]
  - misura: Sostanza Organica su Argilla (%) = Avg([SO/Argilla %]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Sostanza Organica
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Sostanza Organica  ‰ = Avg([Sostanza organica %])*10 [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Sostanza Organica  ‰", "key": "Avg([Sostanza organica %])*10", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": true, "minMax": "max", "min": 0, "max": 400}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: Sostanza Organica  (‰) = Avg([Sostanza organica %])*10 [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Tessitura del terreno vs vini
- **text-image** ''
- **text-image** '^[![XVFkUk](/api/v1/apps/bc52320c\\-8cdd\\-4e26\\-a374\\-2c0958aad934/media/files/Immagine1.png)](center)'

## Tessitura
- **barchart** dims=["=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Argilla  ‰ = Avg(Argilla)*10 [F:#.##0]
  - misura: Limo  ‰ = Avg([Limo])*10 [F:#,##0]
  - misura: Sabbia  ‰ = Avg (Sabbia)*10 [F:#,##0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "stacked"}, "dataPoint": {"showLabels": true, "showSegmentLabels": true, "showTotalLabels": false}, "refLine": [], "color": {"auto": true, "mode": "byExpression", "paletteColor": {"index": 6}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "auto", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "all", "dock": "near", "spacing": 1, "autoMinMax": false, "minMax": "minMax", "min": 0, "max": 1000}, "legend": {"show": true, "dock": "bottom", "showTitle": true}, "qInterColumnSortOrder": [1, 3, 2, 0]}
- **text-image** ''
- **kpi** dims=[]
  - misura: Frazione Limosa  ‰ = Avg([Limo])*10 []
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **kpi** dims=[]
  - misura: Frazione Sabbiosa  ‰ = Avg([Sabbia])*10 []
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **kpi** dims=[]
  - misura: Frazione Argillosa  ‰ = Avg([Argilla])*10 []
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}

## Triangolo USDA
- **text-image** ''
- **kpi** dims=[]
  - misura: Frazione Limosa  ‰ = Avg([Limo])*10 []
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **kpi** dims=[]
  - misura: Frazione Sabbiosa  ‰ = Avg([Sabbia])*10 []
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **kpi** dims=[]
  - misura: Frazione Argillosa  ‰ = Avg([Argilla])*10 []
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** '^[![CCrrgpv](/api/v1/apps/d82cb3cb\\-fbe6\\-4ec3\\-b5d8\\-be4e307ea5ba/media/files/triangolo%20con%20terreno%20USDA.jpg)#[\n]({"index":8,"color":"#ffcf02"})](center)^[#[\n]({"index":8,"color":"#ffcf02"})](c'
- **table** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "NomeVigneto (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "CODICE PUNTO (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "Descrizione (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Argilla ‰ = Avg(Argilla)*10 [F:#.##0]
  - misura: Limo ‰ = Avg(Limo)*10 [F:#.##0]
  - misura: Sabbia ‰ = Avg(Sabbia)*10 [F:#.##0]
  - {"qInterColumnSortOrder": [0, 1, 2, 3, 6, 5, 4]}

## Zinco (Zn) DTPA
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura:  = Avg([Zinco assimilabile (Zn) ppm DTPA]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "", "refLineExpr": {"value": 0.5, "label": "0.5"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "", "refLineExpr": {"value": 1, "label": "1"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "='Zinco (Zn) assimilabile (DTPA) ppm'", "key": "Avg([Zinco assimilabile (Zn) ppm DTPA])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 0, "max": 2}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura:  = Avg([Zinco assimilabile (Zn) ppm DTPA]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Zinco (Zn) EDTA
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=NomeVigneto (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura:  = Avg([Zinco assimilabile (Zn) ppm EDTA]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "min", "refLineExpr": {"value": 0.6, "label": "0.6"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "med", "refLineExpr": {"value": 3.8, "label": "3.8"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "max", "refLineExpr": {"value": 8, "label": "8"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": ""}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "='Zinco (Zn) assimilabile (DTPA) ppm'", "key": "Avg([Zinco assimilabile (Zn) ppm EDTA])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 0, "max": 2}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura:  = Avg([Zinco assimilabile (Zn) ppm EDTA]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## Zolfo (S)
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=NomeVigneto (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: Zolfo (S) assimilabile ppm = Avg([Zolfo assimilabile (S) ppm]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "min", "refLineExpr": {"value": 13, "label": "13"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "max", "refLineExpr": {"value": 20, "label": "20"}, "paletteColor": {"index": -1, "color": "#ff2824"}, "style": {"lineThickness": 2, "lineType": ""}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "Zolfo (S) assimilabile ppm", "key": "Avg([Zolfo assimilabile (S) ppm])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "max", "min": 0, "max": 2.5}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: Zolfo (S) assimilabile (ppm) = Avg([Zolfo assimilabile (S) ppm]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## pH  in KCL
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: pH = Avg([pH in KCL]) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "", "refLineExpr": {"value": 6.8, "label": "6.8"}, "paletteColor": {"index": -1, "color": "#b82d23"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "", "refLineExpr": {"value": 7.2, "label": "7.2"}, "paletteColor": {"index": -1, "color": "#b82d23"}, "style": {"lineThickness": 2, "lineType": ""}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "pH", "key": "Avg([pH in KCL])", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "minMax", "min": 6, "max": 9}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: pH in KCL = Avg([pH in KCL]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## pH in H2O (1)
- **text-image** ''
- **text-image** '^[![RxUnEw](/api/v1/apps/bc52320c\\-8cdd\\-4e26\\-a374\\-2c0958aad934/media/files/Immagine6.png)](center)'
- **text-image** 'Conla misura del pH si possono ipotizzare le disponibilità degli elementinutritivi e l’attività microbiologica del terreno. Conoscendo gli intervalli dipH ottimali per ogni coltura si possono pianific'
- **text-image** '![bVtVHB](/api/v1/apps/bc52320c\\-8cdd\\-4e26\\-a374\\-2c0958aad934/media/files/Immagine7.jpg)'
- **text-image** '^[![Pqdb](/api/v1/apps/bc52320c\\-8cdd\\-4e26\\-a374\\-2c0958aad934/media/files/Immagine9.jpg)](center)'

## pH in H2O
- **barchart** dims=["Vigna (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})", "=[CODICE PUNTO] (ordina {'qSortByNumeric': 1, 'qSortByAscii': 1, 'qSortByLoadOrder': 1, 'qExpression': {}})"]
  - misura: pH = (Avg([pH in H2O])) [F:#.##0,0]
  - {"orientation": "vertical", "barGrouping": {"grouping": "grouped"}, "dataPoint": {"showLabels": true, "showSegmentLabels": false, "showTotalLabels": true}, "refLine": [{"label": "", "refLineExpr": {"value": 6.8, "label": "6.8"}, "paletteColor": {"index": -1, "color": "#b82d23"}, "style": {"lineThickness": 2, "lineType": "8 4"}}, {"label": "", "refLineExpr": {"value": 7.2, "label": "7.2"}, "paletteColor": {"index": -1, "color": "#b82d23"}, "style": {"lineThickness": 2, "lineType": "8 4"}}], "color": {"auto": false, "mode": "byMeasure", "paletteColor": {"index": 6}, "byMeasureDef": {"label": "pH", "key": "(Avg([pH in H2O]))", "type": "expression", "activeMeasureIndex": 0}, "useBaseColors": "off"}, "scrollStartPos": 0, "dimensionAxis": {"continuousAuto": true, "show": "all", "label": "tilted", "dock": "near", "axisDisplayMode": "auto", "maxVisibleItems": 10}, "measureAxis": {"show": "labels", "dock": "near", "spacing": 0.5, "autoMinMax": false, "minMax": "minMax", "min": 6, "max": 9}, "legend": {"show": false, "dock": "auto", "showTitle": true}, "qInterColumnSortOrder": [0, 1, 2]}
- **kpi** dims=[]
  - misura: pH  in H₂0 = Avg([pH in H2O]) [F:#.##0,0]
  - {"showTitles": false, "textAlign": "center", "fontSize": "M"}
- **text-image** ''

## xxxx
- **text-image** ''
- **text-image** '^[%[![uGk](/api/v1/apps/bc52320c\\-8cdd\\-4e26\\-a374\\-2c0958aad934/media/files/Immagine2.png)](5)](center)'
- **text-image** '^[![XZcFDPe](/api/v1/apps/bc52320c\\-8cdd\\-4e26\\-a374\\-2c0958aad934/media/files/Immagine5.jpg)](left)'
