// Copyright (c) Johnny Patterson

package Server_Dueling {

function dsMapManagerSO()
{
	if (isObject(dsMapManagerSO))
		return nameToID(dsMapManagerSO);

	%directory = "config/server/dueling/maps/";

	%obj = new ScriptObject(dsMapManagerSO)
	{
		directory = %directory;
	};

	%maps = new SimGroup();
	%submissions = new SimSet();
	%obj.maps = %maps;
	%obj.submissions = %submissions;

	%list = new GuiTextListCtrl();

	%pattern = %directory @ "*";
	%path = findFirstFile(%pattern);
	%prefixLen = strlen(%directory);

	%weapons = dsWeaponManagerSO.list;
	%weaponCount = getFieldCount(%weapons);

	%instantGroupBackup = $instantGroup;
	$instantGroup = %maps;

	while (isFile(%path))
	{
		%relative = getSubStr(%path, %prefixLen, 1e5);
		%name = fileName(%relative);
		if (fileExt(%path) !$= ".cs" || strstr(%name, "-bricks.cs") != -1 || strstr(%name, ".") == 0)
		{
			%path = findNextFile(%pattern);
			continue;
		}

		%fields = strreplace(%relative, "/", "\t");
		%count = getFieldCount(%fields);

		if (%count == 2)
		{
			%bl_id = getField(%fields, 0);

			if ((%bl_id | 0) == %bl_id)
			{
				%before = %maps.getCount();
				exec(%path);

				if (%before != %maps.getCount())
				{
					%map = %maps.getObject(%before);
					%record = dsStatManagerSO.getRecordFromID(%bl_id, 1);
					%record.maps.add(%map);

					if (%map.state == 2)
					{
						for (%i = 0; %i < %weaponCount; %i++)
						{
							%weapon = getField(%weapons, %i);

							if (%map.target[%weapon.getName()])
								%weapon.maps.add(%map);
						}
					}
					else if (%map.state == 1)
					{
						%submissions.add(%map);
					}
				}
				else
				{
					error("Map did not load correctly: \"" @ %path @ "\"");
				}
			}
		}

		%path = findNextFile(%pattern);
	}

	$instantGroup = %instantGroupBackup;

	%list.delete();

	return %obj;
}

function dsMapManagerSO::createAndSave(%this, %client, %name)
{
	%map = new ScriptObject()
	{
		class = dsMapSO;
		submitterID = %client.bl_id;
		submitterName = %client.name;
		name = %name;
		state = 0;
	};

	%this.maps.add(%map);
	%client.statRecord.maps.add(%map);
	%path = %this.directory @ %client.bl_id @ "/" @ %name @ "-bricks.cs";
	%partition = %client.partition;

	if (!%partition.saveBricks(%path, 1, 1))
	{
		%map.delete();
		return 0;
	}

	%map.colorSet = %partition.colorSet.name;
	%map.owners = %partition.saveOwners;
	%map.worldBox = %partition.saveWorldBox;
	%map.saveMap();

	return %map;
}

function dsMapManagerSO::findSave(%this, %client, %name)
{
	%group = %client.statRecord.maps;
	%count = %group.getCount();

	for (%i = 0; %i < %count; %i++)
	{
		%map = %group.getObject(%i);

		if (strlwr(%map.name) $= strlwr(%name))
			return %map;
	}

	return 0;
}

function dsMapManagerSO::addSubmission(%this, %map)
{
	%this.submissions.add(%map);

	%group = nameToID(ClientGroup);
	%count = %group.getCount();

	for (%i = 0; %i < %count; %i++)
	{
		%client = %group.getObject(%i);

		if (!%client.isAdmin || %client.isAIControlled())
			continue;

		commandToClient(%client, 'dcSetSubmission', %map, %map.name);
	}
}

function dsMapManagerSO::removeSubmission(%this, %map)
{
	%this.submissions.remove(%map);

	%group = nameToID(ClientGroup);
	%count = %group.getCount();

	for (%i = 0; %i < %count; %i++)
	{
		%client = %group.getObject(%i);

		if (!%client.isAdmin || %client.isAIControlled())
			continue;

		commandToClient(%client, 'dcRemoveSubmission', %map);
	}
}

function dsMapManagerSO::sendSubmissions(%this, %client)
{
	%submissions = %this.submissions;
	%count = %submissions.getCount();

	for (%i = 0; %i < %count; %i++)
	{
		%map = %submissions.getObject(%i);

		commandToClient(%client, 'dcSetSubmission', %map, %map.name);
	}
}

function dsMapSO::getOwnersPrettyString(%this)
{
	%owners = %this.owners;
	%count = getFieldCount(%owners);

	if (!%count)
		return "";

	%field = getField(%owners, 0);
	%id = getWord(%field, 0);
	%name = removeWord(%field, 0);

	%str = %name @ " (" @ %id @ ")";

	if (%count > 1)
	{
		for (%i = 1; %i < %count; %i++)
		{
			%field = getField(%owners, %i);
			%id = getWord(%field, 0);
			%name = removeWord(%field, 0);

			%str = %str @ ", " @ %name @ " (" @ %id @ ")";
		}
	}

	return %str;
}

function dsMapSO::saveMap(%this)
{
	%this.save(dsMapManagerSO.directory @ %this.submitterID @ "/" @ %this.name @ ".cs");
}

}; // package Server_Dueling
