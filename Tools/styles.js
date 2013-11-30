var Styles = {
	"wall": {
		"fillStyle": "grey"
	},
	"free": {
		"fillStyle": "#ddd",
		"strokeStyle": "#ccc"
	},
	"way": {
		"fillStyle": "#000044",
		"textStyle": "white"
	},
	"attacked": {
		"fillStyle": "#ffeeee"
	},
	"attacked_on_kneel": {
		"indicator2Style": "blue"
	},
	"canattack": {
		"strokeStyle": "green"
	},
	"empty": {
		"fillStyle": "#664"
	},
	"enemy": {
		
		"textStyle": "red",
	},
	"noticed" :{
		"textStyle": "pink",
	},
	"not_interacted": {
		"textStyle": "gold"
	},
	"friend": {
		"textStyle": "green"
	},
	
	"low": {
		"fillStyle": "#ccc"
	},
	"high": {
		"fillStyle": "#444"
	},
	"medium": {
		"fillStyle": "#888"
	},
	"visible": {
		"fillStyle": "white"
	},
	"visibleBack": {
		"indicator1Style": "green"
	}
};

function dump(table, allData) {
	var lines = [];
	for (var y = 0; y < table[0].length; y++) {
		var line = "";
		for (var x = 0; x < table.length; x++) {
			var cell = table[x][y];
			var c = " ";
			if (cell.class && cell.class.indexOf("wall") > -1) {
				c = "x";
				if (cell.class.indexOf("low") > -1) c = "_";
			}
			else if (cell.class && cell.class.indexOf("friend") > -1 ) {
				c = cell.text.substring(0,1);
			}
			else if (cell.class && cell.class.indexOf("enemy") > -1 ) {
				c = cell.text.substring(0,1).toUpperCase();
			}
			line+=c;
		}
		lines.push(line);
	}
	return lines;
}

function troopers(alltroopers, active, isfriend) {
	var ts = alltroopers.filter(function(t) { return t.friend == isfriend});
	ts.sort(function(a,b) { return a.type == b.type ? 0 : (a.type > b.type ? 1 : -1) });
	return ts.map(function(t) {
		var a = t.type + "[+" + t.hitpoints + "]\tactions=" + t.points + "\t";
		a += t.medkit ? "m " : ". ";
		a += t.grenade ? "g " : ". ";
		a += t.ration ? "r " : ". ";
		if (t.ghost) a = "*" + a; else a = " " + a;
		if (t.interacted || isfriend) a = " " + a; else a = "~" + a;
		if (t.type == active) a = " > " + a; else if (t.sick == 1) a = "!- " + a; else a = "   " + a;
		return a;
	});
	return ts;
}


$(function() {
	var alies = $("<pre style='color: darkgreen'></pre>");
	var enemies = $("<pre style='color: red'></pre>");
	$("#log").before(alies).before(enemies);
	postRender = function(table, allData) {
		alies.html(troopers(allData.troopers, allData.active, 1).join("\n"));
		enemies.html(troopers(allData.troopers, allData.against, 0).join("\n"));
	}
	if (format == 'visibility') {
			
		function visible(x1, y1, x2, y2, stance) {
			return data.visibility[x1 * height * width  * height * 3 + y1 * width * height * 3 + x2 * height * 3 + y2 * 3 + stance]
		}
		
		function countVisibility(x1, y1, stance) {
			var res = {fromOnly: 0, toOnly: 0, total: 0};
			for (var c = 0; c < data.map.length; c++) {
				for (var r = 0; r < data.map[c].length; r++) {		
					if ((x1-c)*(x1-c) + (y1-r)*(y1-r) < 100) {
						if (data.map[c][r].class.indexOf("free") == 0) {
							res.total++;
							var visibleFrom = (visible(x1, y1, c, r, stance));
							var visibleTo = (visible(c, r, x1, y1, stance));
							if (visibleFrom && !visibleTo) res.fromOnly++;
							if (!visibleFrom && visibleTo) res.toOnly++;
						}
					}
				}
			}
			return res;
		}
		
		function onChange() {
			var stance1 = $("#step option:selected").attr("data-stance") |0;
			var stance2 = $("#step2 option:selected").attr("data-stance") |0;
			var stance = Math.min(stance1, stance2);
			for (var c = 0; c < data.map.length; c++) {
				for (var r = 0; r < data.map[c].length; r++) {	
					if (data.map[c][r].class.indexOf("free") == 0) {
						var vis = countVisibility(c, r, stance);
						//toOnly > 0 - danger
						//toOnly = 0 && fromOnly = 0 - normal
						//toOnly = 0 && fromOnly > 0 - yeah!
						var score = 0;
						if (vis.toOnly > 0) {
							score = vis.toOnly;
						}
						else if (vis.toOnly == 0 && vis.fromOnly == 0) {
							score = 0;
						}
						else {
							score = -vis.fromOnly;
						}
						data.map[c][r].text = score;	//could be attacked from # points - # points to be attacked
					}
				}
			}
		}
		/*
		onChange();
		$("#step").change(function() {onChange();});
		$("#step2").change(function() {onChange();});	*/
	}
});