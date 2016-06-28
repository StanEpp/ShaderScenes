var node = PADrend.loadScene("ThesisStanislaw/ShaderScenes/scenes/crytek-sponza-minsg/sponza.minsg");
var approxNode = PADrend.loadScene("ThesisStanislaw/ShaderScenes/scenes/sponzaApprox/sponza_Approx_55k.dae");
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
photonSampler.setPhotonNumber(10);
//photonSampler.deactivate();

var phongGI = new MinSG.ThesisStanislaw.PhongGI;
phongGI.setPhotonSampler(photonSampler);
//phongGI.setPhotonRenderer(photonRenderer);

var photonRenderer = new MinSG.ThesisStanislaw.PhotonRenderer();
photonRenderer.setApproximatedScene(approxNode);
photonRenderer.setSamplingResolution(16,16);
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

static approxPolyCount = MinSG.countTriangles(approxNode);

static displayPolygonIDTexture = fn(texture) {
  var tmpTexture = Rendering.createStdTexture(texture.getWidth(), texture.getHeight(), false);
  {
    var pa = Rendering.createColorPixelAccessor(renderingContext, texture);  
    var pa2 = Rendering.createColorPixelAccessor(renderingContext, tmpTexture);  
    for(var y=0;y<pa.getHeight();y++) {  
      for(var x=0;x<pa.getWidth();x++) {
        pa2.writeSingleValueFloat(x,y, pa.readSingleValueFloat(x,y)/approxPolyCount);
      }
    }
  }
  Rendering.showDebugTexture(tmpTexture, 1);
};

static displayLightPathTBO = fn(texture) {
  var width = approxPolyCount.sqrt().ceil();
  var tmpTexture = Rendering.createStdTexture(width, width, false);
  {
    var pa = Rendering.createColorPixelAccessor(renderingContext, texture);  
    var pa2 = Rendering.createColorPixelAccessor(renderingContext, tmpTexture);  
    for(var y=0;y<width;y++) {  
      for(var x=0;x<width;x++) {
        if(y*width+x < approxPolyCount) {
          pa2.writeSingleValueFloat(x,y, pa.readSingleValueFloat(y*width+x,0)/approxPolyCount);
          //out(pa.readSingleValueFloat(y*width+x,0), " ");
        }
      }
      //outln();
    }
  }
  Rendering.showDebugTexture(tmpTexture, 1);
};

static displaySamplingTexture = fn(texture) {
  var tmpTexture = Rendering.createStdTexture(texture.getWidth(), texture.getHeight(), false);
  {
    var pa = Rendering.createColorPixelAccessor(renderingContext, texture);  
    var pa2 = Rendering.createColorPixelAccessor(renderingContext, tmpTexture);  
    for(var y=0;y<pa.getHeight();y++) {  
      for(var x=0;x<pa.getWidth();x++) {
        pa2.writeSingleValueFloat(x,y, pa.readSingleValueFloat(x,y)/100);
        out(pa.readSingleValueFloat(x,y).toIntStr(), " ");
        /*out(pa.readColor4ub(x,y).r(), ",");
        out(pa.readColor4ub(x,y).g(), ",");
        out(pa.readColor4ub(x,y).b(), ",");
        out(pa.readColor4ub(x,y).a(), " ");*/
      }
      outln();
    }
  }
  Rendering.showDebugTexture(tmpTexture, 1);
};

static getBaseTypeEntries = fn( obj, baseType=void ){
	return	gui.createComponents( {	
		GUI.TYPE 		: 	GUI.TYPE_COMPONENTS, 
		GUI.PROVIDER	:	'NodeEditor_ObjConfig_' + (baseType ? baseType : obj.getType().getBaseType()).toString(), 
		GUI.CONTEXT		:	obj 
	});
};

Std.module.on('PADrend/gui',fn(gui) {
  gui.register('NodeEditor_ObjConfig_' + MinSG.ThesisStanislaw.LightPatchRenderer, fn(MinSG.ThesisStanislaw.LightPatchRenderer state) {
  	var entries = getBaseTypeEntries(state);
  	entries += "*LightPatchRenderer:*";
  	entries += GUI.NEXT_ROW;
    var idtexture = state.getPolygonIDTexture();    
		entries += {
			GUI.TYPE : GUI.TYPE_BUTTON,
			GUI.LABEL : "Show ID texture",
			GUI.ON_CLICK : [idtexture] => displayPolygonIDTexture,
			GUI.TOOLTIP : "Show the texture for 0.5 sek in the\n lower left corner of the screen."
		};
  	entries += GUI.NEXT_ROW;
    var ntexture = state.getNormalTexture();    
		entries += {
			GUI.TYPE : GUI.TYPE_BUTTON,
			GUI.LABEL : "Show Normal Texture",
			GUI.ON_CLICK : [ntexture,2] => Rendering.showDebugTexture,
			GUI.TOOLTIP : "Show the texture for 0.5 sek in the\n lower left corner of the screen."
		};
  	entries += GUI.NEXT_ROW;
    var ltexture = state.getLightPatchTBO();    
		entries += {
			GUI.TYPE : GUI.TYPE_BUTTON,
			GUI.LABEL : "Show Light Patch TBO",
			//GUI.ON_CLICK : [ltexture,2,100] => Rendering.showDebugTexture,
			GUI.ON_CLICK : [ltexture] => displayLightPathTBO,
			GUI.TOOLTIP : "Show the texture for 0.5 sek in the\n lower left corner of the screen."
		};
  	
  	entries += GUI.NEXT_ROW;
  	return entries;
  });
});
// ----


Std.module.on('PADrend/gui',fn(gui) {
  gui.register('NodeEditor_ObjConfig_' + MinSG.ThesisStanislaw.PhotonRenderer, fn(MinSG.ThesisStanislaw.PhotonRenderer state) {
  	var entries = getBaseTypeEntries(state);
  	entries += "*PhotonRenderer:*";
  	entries += GUI.NEXT_ROW;
    var texture = state.getLightTexture();    
		entries += {
			GUI.TYPE : GUI.TYPE_BUTTON,
			GUI.LABEL : "Show Light Texture",
			GUI.ON_CLICK : [texture,1] => Rendering.showDebugTexture,
			GUI.TOOLTIP : "Show the texture for 0.5 sek in the\n lower left corner of the screen."
		};
  	entries += GUI.NEXT_ROW;
    var ntexture = state.getNormalTexture();    
		entries += {
			GUI.TYPE : GUI.TYPE_BUTTON,
			GUI.LABEL : "Show Normal Texture",
			GUI.ON_CLICK : [ntexture,1] => Rendering.showDebugTexture,
			GUI.TOOLTIP : "Show the texture for 0.5 sek in the\n lower left corner of the screen."
		};
  	
  	entries += GUI.NEXT_ROW;
  	return entries;
  });
});
// ----

Std.module.on('PADrend/gui',fn(gui) {
  gui.register('NodeEditor_ObjConfig_' + MinSG.ThesisStanislaw.PhotonSampler, fn(MinSG.ThesisStanislaw.PhotonSampler state) {
  	var entries = getBaseTypeEntries(state);
  	entries += "*PhotonSampler:*";
  	entries += GUI.NEXT_ROW;
    var ptexture = state.getPosTexture();    
		entries += {
			GUI.TYPE : GUI.TYPE_BUTTON,
			GUI.LABEL : "Show Position Texture",
			GUI.ON_CLICK : [ptexture] => Rendering.showDebugTexture,
			GUI.TOOLTIP : "Show the texture for 0.5 sek in the\n lower left corner of the screen."
		};
  	entries += GUI.NEXT_ROW;
    var ntexture = state.getNormalTexture();    
		entries += {
			GUI.TYPE : GUI.TYPE_BUTTON,
			GUI.LABEL : "Show Normal Texture",
			GUI.ON_CLICK : [ntexture] => Rendering.showDebugTexture,
			GUI.TOOLTIP : "Show the texture for 0.5 sek in the\n lower left corner of the screen."
		};
  	entries += GUI.NEXT_ROW;
    var stexture = state.getSamplingTexture();    
		entries += {
			GUI.TYPE : GUI.TYPE_BUTTON,
			GUI.LABEL : "Show Sampling Texture",
			GUI.ON_CLICK : [stexture] => displaySamplingTexture,
			GUI.TOOLTIP : "Show the texture for 0.5 sek in the\n lower left corner of the screen."
		};
  	entries += GUI.NEXT_ROW;
    var mtexture = state.getMatrixTexture();    
		entries += {
			GUI.TYPE : GUI.TYPE_BUTTON,
			GUI.LABEL : "Show Matrix Texture",
			GUI.ON_CLICK : [mtexture,1] => Rendering.showDebugTexture,
			GUI.TOOLTIP : "Show the texture for 0.5 sek in the\n lower left corner of the screen."
		};
  	
  	entries += GUI.NEXT_ROW;
  	return entries;
  });
});
// ----