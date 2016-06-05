var node = PADrend.loadScene("ThesisStanislaw/ShaderScenes/scenes/crytek-sponza-minsg/sponza.minsg");
var approxNode = PADrend.loadScene("ThesisStanislaw/ShaderScenes/scenes/sponzaApprox/sponza_Approx.dae");
approxNode.removeStates();

var lights = MinSG.collectLightNodes(node);
var filter = fn(light) { return light.getLightType() == MinSG.LightNode.SPOT; };
var spotLights = lights.filter(filter);

var indexingState = new MinSG.ThesisStanislaw.PolygonIndexingState;
//indexingState.setDebugOutput(true);

var lightPatchRenderer = new MinSG.ThesisStanislaw.LightPatchRenderer;
lightPatchRenderer.setSpotLights(spotLights);
lightPatchRenderer.setApproximatedScene(approxNode);
lightPatchRenderer.setSamplingResolution(512, 512);
lightPatchRenderer.setCamera(PADrend.getActiveCamera());

var photonSampler = new MinSG.ThesisStanislaw.PhotonSampler;
photonSampler.setApproximatedScene(approxNode);
photonSampler.setCamera(PADrend.getActiveCamera());
photonSampler.setPhotonNumber(100);
//photonSampler.deactivate();

var phongGI = new MinSG.ThesisStanislaw.PhongGI;
//phongGI.setPhotonSampler(photonSampler);

var photonRenderer = new MinSG.ThesisStanislaw.PhotonRenderer();
photonRenderer.setApproximatedScene(approxNode);
photonRenderer.setSamplingResolution(32,32);
photonRenderer.setLightPatchRenderer(lightPatchRenderer); 
photonRenderer.setPhotonSampler(photonSampler);
photonRenderer.setSpotLights(spotLights);

var approxRenderer = new MinSG.ThesisStanislaw.ApproxSceneDebug;
approxRenderer.setApproximatedScene(approxNode);
approxRenderer.setLightPatchRenderer(lightPatchRenderer);

approxNode.addState(indexingState);
approxNode.addState(approxRenderer);
approxRenderer.deactivate();

node.addState(lightPatchRenderer);
node.addState(photonSampler);
node.addState(photonRenderer);
node.addState(phongGI);

PADrend.selectScene(node);
