import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const { setModuleImports, getAssemblyExports, getConfig } = 
	await dotnet.create();

// define JS exports (which can be imported into C#)
setModuleImports("main.js", {
  
});

// obtain exported C# object
const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
const demoFileProgram = exports.DemoFile.Example.WebAssembly.Program;

// print string returned from C#
const greetingText = demoFileProgram.Greeting();
console.log(greetingText);

// setup button click
window.document.getElementById("OpenDemoFileButton").onclick = openDemoFile;

// setup update function - this function will be called on every repaint - frequency will match display refresh rate
//window.requestAnimationFrame(update);
setTimeout(update, 0);


function openDemoFile()
{
	let input = document.createElement('input');
    input.type = 'file';
    input.onchange = _ => {
        let files = Array.from(input.files);
        console.log(files);
		readFile(files[0]);
    };
    input.click();

    input.remove();
}

function readFile(file) {
	document.getElementById("demo_parse_result").innerHTML = "";
	var reader = new FileReader ();
	reader.onload = async function (e) {
		var arrayBuf = reader.result;
		console.log(arrayBuf.byteLength);
        var resultStr = await demoFileProgram.ParseToEnd(new Uint8Array(arrayBuf));
		document.getElementById("demo_parse_result").innerHTML = resultStr;
	}
	reader.readAsArrayBuffer(file);
}

function update() {

	demoFileProgram.Update();

	setTimeout(update, 0);
}
