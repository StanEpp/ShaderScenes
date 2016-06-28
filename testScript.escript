var scene = PADrend.loadScene("ThesisStanislaw/ShaderScenes/scenes/crytek-sponza-minsg/sponza.minsg");

var lights = MinSG.collectLightNodes(scene);
var filter = fn(light) { return light.getLightType() == MinSG.LightNode.SPOT; };
var spotLights = lights.filter(filter);

var indexingState = new MinSG.ThesisStanislaw.PolygonIndexingState;

var lightPatchRenderer = new MinSG.ThesisStanislaw.LightPatchRenderer;
lightPatchRenderer.setSpotLights(spotLights);
lightPatchRenderer.setSamplingResolution(512, 512);

var photonSampler = new MinSG.ThesisStanislaw.PhotonSampler;
photonSampler.setCamera(PADrend.getActiveCamera());

var phongGI = new MinSG.ThesisStanislaw.PhongGI;
phongGI.setPhotonSampler(photonSampler);

var photonRenderer = new MinSG.ThesisStanislaw.PhotonRenderer();
photonRenderer.setLightPatchRenderer(lightPatchRenderer); 
photonRenderer.setPhotonSampler(photonSampler);
photonRenderer.setSpotLights(spotLights);

scene.addState(lightPatchRenderer);
scene.addState(photonSampler);
scene.addState(photonRenderer);
scene.addState(phongGI);

PADrend.selectScene(scene);

var path = WaypointsPlugin.loadPath("ThesisStanislaw/ShaderScenes/scenes/crytek-sponza-minsg/Walkthrough.path");

var approxPolygons = [5, 10, 15, 20, 25, 35, 45, 55, 65, 85, 105, 125, 145, 165, 185, 205, 230];
var photonNumbers = [10, 20, 30, 40, 50, 60, 70, 80, 90, 100];
var samplingResolutions = [16, 32, 64, 128, 256, 512];

foreach(samplingResolutions as var samplingRes){
	photonRenderer.setSamplingResolution(samplingRes,samplingRes);
	
	foreach (photonNumbers as var photonNumber){
		photonSampler.setPhotonNumber(photonNumber);
		
		foreach (approxPolygons as var numPolygons){
			outln("Settings: " + numPolygons + "k Polygons ; " + photonNumber + " Photons ; " + samplingRes +"x"+ samplingRes + " Resolution");
			
			var approxScene = PADrend.loadScene("ThesisStanislaw/ShaderScenes/scenes/sponzaApprox/sponza_Approx_" + numPolygons + "k.dae");
			lightPatchRenderer.setApproximatedScene(approxScene);
			photonSampler.setApproximatedScene(approxScene);
			photonRenderer.setApproximatedScene(approxScene);
			indexingState.reupdatePolygonIDs(); 
			approxScene.addState(indexingState);
			
			PADrend.getDolly().setRelTransformation(path.getWorldPosition(0));
			
			var statistics = PADrend.frameStatistics;

			var data = [];
			for(var i=0;i<statistics.getNumCounters();i++)
				data+=[];

			var stepSize = path.getMaxTime() / 10000;

			// run along path
			var stop=false;
			for(var time = 0; time<=path.getMaxTime() && !stop; time+=stepSize){
				PADrend.getDolly().setRelTransformation(path.getWorldPosition(time));
				frameContext.beginFrame();
				PADrend.renderScene( PADrend.getRootNode(), PADrend.getActiveCamera(), PADrend.getRenderingFlags(), PADrend.getBGColor(), PADrend.getRenderingLayers());
				frameContext.endFrame(true);
				PADrend.getEventQueue().process();
				while(PADrend.getEventQueue().getNumEventsAvailable() > 0) {
					var evt = PADrend.getEventQueue().popEvent(); // stop on key
					if(evt.type == Util.UI.EVENT_KEYBOARD && evt.pressed)
						stop=true;
				}
				for(var i=0;i<statistics.getNumCounters();i++)
					data[i]+=statistics.getValue(i);
				PADrend.SystemUI.swapBuffers();
			}

			// export data
			var table = new (Std.module('LibUtilExt/DataTable'))( "frame" );
			for(var i=0;i<statistics.getNumCounters();i++){
				if(statistics.getDescription(i)=="?") continue;
				table.addDataRow(statistics.getDescription(i),statistics.getUnit(i),data[i],"#ff0000" );
			}
			table.exportCSV("benchmarks/bench" + numPolygons + "k_" +  photonNumber + "P_" + samplingRes + "R.csv");
			
			PADrend.deleteScene(approxScene);
		}
	}
}