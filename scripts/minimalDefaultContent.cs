// Copyright (c) Johnny Patterson

package Server_Dueling {

function DisabledDataBlockGroup()
{
	%group = "/DisabledDataBlockGroup";

	if (isObject(%group))
		return %group;

	RootGroup.add(new SimGroup(DisabledDataBlockGroup));

	// %group.add(brick2x2x5girderData);
	// %group.add(brick32x32froadcData);
	// %group.add(brick32x32froadsData);
	// %group.add(brick32x32froadtData);
	// %group.add(brick32x32froadxData);
	// %group.add(brick3x1x7WallData);
	// %group.add(brick4x1x2FenceData);
	// %group.add(brick4x1x5windowData);
	%group.add(brickMusicData);
	// %group.add(brickPineTreeData);
	%group.add(brickVehicleSpawnData);
}

}; // package Server_Dueling
