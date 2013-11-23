var troops = ['commander', 'soldier', 'medic'];
var troopSettings = {
	commander: {
		points: 10,
		commander_bonus: 0,
		visible: 8,
		attack: 7,
		damage1: 15,
		damage2: 20,
		damage3: 25,
		shootCost: 3,
		move1: 2,
		move2: 4,
		move3: 6,
		grenadeAttack: 5,
		grenadeCost: 8,
		addPoints: 3,
		healedByMedic: 5
	},
	soldier: {
		points: 10,
		commander_bonus: 2,
		visible: 7,
		attack: 8,
		damage1: 25,
		damage2: 30,
		damage3: 35,
		shootCost: 4,
		move1: 2,
		move2: 4,
		move3: 6,
		grenadeAttack: 5,
		grenadeCost: 8,
		addPoints: 3,
		healedByMedic: 5
	},
	medic: {
		points: 10,
		commander_bonus: 2,
		visible: 7,
		attack: 5,
		damage1: 9,
		damage2: 12,
		damage3: 15,
		shootCost: 2,
		move1: 2,
		move2: 4,
		move3: 6,
		grenadeAttack: 5,
		grenadeCost: 8,
		addPoints: 3,
		healedByMedic: 3
	}
};
var positions = [1,2,3];

troops.forEach(function(t) {
	var s = troopSettings[t];
	s.shootsPerTurn = Math.floor(s.points / s.shootCost);
	s.afterFullShootPoints = s.points - s.shootsPerTurn;
	for (var p = 1; p <= 3; p++) {
		s["fullDamage"+p] = s.shootsPerTurn * s["damage"+p];
		s["fullDamageWithHeal"+p] = Math.max(0, s.shootsPerTurn * s["damage"+p] - (troopSettings.medic.points * troopSettings.commander.healedByMedic));
	}
	
});

function distance(from, to) {
	var dx = from.x - to.x, dy = from.y - to.y;
	return Math.sqrt(dx*dx + dy*dy);
}
/*
troops: {
	all: [],
	visible: [],
	attackable: [],
	attackers: [],
	healable: [],
	alies: []
	{id: 1, x, y, hitspoints, points, type, healerNear, hasMedcit, hasGrenade},
	{id: 2, x, y, hitspoints, points, type, healerNear, hasMedcit, hasGrenade}
},
distances: [[3,4],[4,3]]
*/
var actions = [{
	name: "just shoot",
	isPossible: function(self, troops) {
		return troops.attackable.length > 0;
	},
	
	apply: function(self, troops) {
		var toAttack = troops.attackable[0];
		var actions = [];
		for (var i = 0; i < self.shootsPerTurn; i++) {
			actions.push({action: 'shoot', target: toAttack});
		}
		return actions;
	}
},
{
	name: "shoot and step back",
	isPossible: function(self, troops) {
		return troops.attackable.length > 0;
	},
	
	apply: function(self, troops) {
		var toAttack = troops.attackable[0];
		var actions = [];
		var pointsLeft = self.points;
		while (pointsLeft - self.shootCost >= self.move1) {
			actions.push({action: 'shoot', target: toAttack});
			pointsLeft -= self.shootCost;
		}
		var dx = self.x - toAttack.x;
		var dy = self.y - toAttack.y;
		if (Math.abs(dx) > Math.abs(dy)) {
			actions.push({action: 'move', target: {x: self.x + (dx / Math.abs(dx)), y: self.y}});
		}
		else {
			actions.push({action: 'move', target: {x: self.x, y: self.y + (dy / Math.abs(dy))}});
		}
		return actions;
	}
},
{
	name: "shoot from sit",
	isPossible: function(self, troops) {
		return troops.attackable.length > 0;
	},	
	apply: function(self, troops) {
		var toAttack = troops.attackable[0];
		var actions = [];
		var pointsLeft = self.points;
		actions.push({action: 'down', target:{}});
		pointsLeft -= 2;
		while (pointsLeft - self.shootCost >= 0) {
			actions.push({action: 'shoot', target: toAttack});
			pointsLeft -= self.shootCost;
		}
		if (pointsLeft >= 2) {
			actions.push({action: 'up', target:{}});
		}
		return actions;
	}	
}
];

function fieldToTroops(field, myX, myY) {
	var allTroops = [];
	for (var y = 0; y < field.length; y++) {
		for (var x = 0; x < field[y].length; x++) {
			if (field[y][x].side) {
				troop = {id: allTroops.length};
				troop.x = x;
				troop.y = y;
				troop.side = field[y][x].side;
				troop.type = field[y][x].type;
				troop.hits = field[y][x].hits || 100;
				for (var prop in troopSettings[troop.type]) {
					troop[prop] = troopSettings[troop.type][prop];
				}
				
				allTroops.push(troop);
			}
		}
	}
	var self = allTroops.filter(function(t) {return t.x == myX && t.y == myY})[0];
	return recalculateTroops(self, allTroops);
}

function recalculateTroops(self, allTroops) {
	var troops = {all: [], visible: [], attackable: [], attackers: [], alies: [], healable: []};
	allTroops.forEach(function(troop) {
		troops.all.push(troop);
		if (troop.side == 'enemy') {
			if (distance(self, troop) <= self.attack) {
				troops.attackable.push(troop);
			}
			if (distance(self, troop) <= troop.attack) {
				troops.attackers.push(troop);
			}
			if (distance(self, troop) <= self.visible) {
				troops.visible.push(troop);
			}
			if (troop.hasMedkit || troop.healerNear) {
				troops.healable.push(troop);
			}
		}
		else {
			troops.alies.push(troop);
			if (distance(self, troop) == 1) {
				if (self.type == 'medic' || self.hasMedkit) {
					troops.healable.push(troop);
				}
				if (troop.type == 'medic' || troop.hasMedkit) {
					troops.healable.push(self);
				}
			}
		}
	});
	troops.self = self;
	return troops;
}

function applyActions(self, troops, actions) {
	var position = self.position || 1;
	self.newX = self.x;
	self.newY = self.y;
	troops.all.forEach(function(t) {
		t.newHits = t.hits;
		t.newAlive = true;
		t.newX = t.x;
		t.newY = t.y;
	});
	actions.forEach(function(a) {
		switch (a.action) {
			case 'shoot':
				a.target.newHits = a.target.newHits - self["damage"+position];
				a.target.newAlive = a.target.newHits > 0;
				break;
			case 'move':
				self.newX = a.target.x;
				self.newY = a.target.y;
				break;
			case 'up':
				position--;
				break;
			case 'down':
				position++;
				break;
			case 'grenade':
				a.target.newHits = a.target.hits = self.grenadeDamage;
				a.target.newAlive = a.target.newHits > 0;
				break;
		}
	});
	var newTroops = recalculateTroops({x: self.newX, y: self.newY, visible: self.visible, attack: self.attack, self:self}, troops.all);
	self.newHits = self.hits;
	newTroops.attackers.forEach(function(a) {
		self.newHits -= a.fullDamage1;
	});
	newTroops.healable.forEach(function(a) {
		if (a.self) a = self;
		if (a.newHits > 0) a.newHits = Math.min(a.newHits + 50, 100);
	});
	if (self.newHits <=0) self.newAlive = false;
	var changes = {
		enemyDamage: 0,
		enemyLost: 0,
		ourDamage: 0,
		ourLost: 0,
		attackersDelta: newTroops.attackers.length - troops.attackers.length,
		attackableDelta: newTroops.attackable.length - troops.attackable.length
	}
	troops.all.forEach(function(t) {
		if (t.newAlive === false) {
			changes[t.side == 'my' ? 'ourLost' : 'enemyLost']++;
		}
		else if (t.newHits != undefined) {
			changes[t.side == 'my' ? 'ourDamage' : 'enemyDamage'] += (t.hits - t.newHits)
		}
	});
	return changes;
	
	
}

function calculateRank(changes) {
	//larger ==> better
	var rank = (10000000 - 1000000*changes.ourLost) + (100000*changes.enemyLost) + (100*changes.enemyDamage - 100*changes.ourDamage) + (10*(-changes.attackersDelta) + changes.attackableDelta);
	return rank;
}

function createLogger(el) {
	el.html("");
	function append(txt) {
		el.html(el.html() + "\n\n" + txt);
	}
	return {
		onAction: function(name, actions, changes, rank) {
			if (actions == null) { return append("Try " + name + ": impossible"); }
			append("Try " + name);			
			append(JSON.stringify(actions.map(function(a) {return {action:a.action, x:a.target.x, y:a.target.y, type:a.target.type}}), null, 4));
			append(JSON.stringify(changes, null, 4));
			append("Rank: " + rank);
		},
		best: function(c) {
			append("Better is " + c.name);
		}
	}
}


function analyze(self, troops, logger) {
	var toCompare = [];
	actions.forEach(function(a) {
		if (a.isPossible(self, troops)) {
			var actions = a.apply(self, troops);
			var changes = applyActions(self, troops, actions);
			var rank = calculateRank(changes);
			logger.onAction(a.name, actions, changes, rank);
			toCompare.push({name: a.name, actions: actions, rank: rank});
		}
		else {
			logger.onAction(a.name);
		}
	});
	var best = toCompare.sort(function(c1, c2) { return c2.rank - c1.rank;})[0];
	logger.best(best);
	return best;
}

