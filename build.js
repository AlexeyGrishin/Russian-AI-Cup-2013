var fs = require('fs');


function processAndCopy(dir, file) {
    var contents = fs.readFileSync(dir + file, "utf8").split("\n");
    contents = contents.filter(function(c) { return c.indexOf('[DEBUG]') == -1 });
    fs.writeFileSync("Build/" + dir + file, contents.join("\n"), "utf8");
}

function processCsFiles(dir) {
  var bdir = "Build" + (dir == '.' ? '' : '/' + dir);
  var prevFiles = fs.readdirSync(bdir);
  prevFiles.forEach(function(f) { if (f.slice(-3) == '.cs') fs.unlink( bdir + "/" + f)});

  var files = fs.readdirSync(dir);

  for (var i = 0; i < files.length; i++) {
    if (files[i].slice(-3) == '.cs') {
      processAndCopy(dir == '.' ? '' : dir + '/', files[i]);
    }
  }
}

processCsFiles(".");