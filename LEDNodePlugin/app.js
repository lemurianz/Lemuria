/****************************************
* instantiate our lights.
* The first argument is an IP or hostname.
* The second argument is an optional name used when printing information to the console.
******************************************************************/
var led = require('./LemuriaLightController.js');
var LemuriaLight = new led.wifi370('192.168.0.59', 'LemuriaLight');
//LemuriaLight.writeToLight(200, 0, 0, 100); 
//# sourceMappingURL=app.js.map