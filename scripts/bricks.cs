// Copyright (c) Johnny Patterson

if ($Error::AddOn_NotFound == ForceRequiredAddOn("Brick_Large_Cubes"))
	return;

datablock fxDTSBrickData(brick64xBoundaryCubeData : brick64xCubeData)
{
	category = "Special";
	isWaterBrick = true;
	subCategory = "Utility";
	uiName = "64x Boundary Cube";
};

function brick64xBoundaryCubeData::onPlant(%this, %brick)
{
	%brick.setColliding(1);
	%brick.setRayCasting(0);
	%brick.setRendering(0);
}

function brick64xBoundaryCubeData::onLoadPlant(%this, %brick)
{
	%brick.setColliding(1);
	%brick.setRayCasting(0);
	%brick.setRendering(0);
}
