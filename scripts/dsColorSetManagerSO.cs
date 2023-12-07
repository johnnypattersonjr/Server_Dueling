// Copyright (c) Johnny Patterson

package Server_Dueling {

function dsColorSetManagerSO()
{
	if (isObject(dsColorSetManagerSO))
		return nameToID(dsColorSetManagerSO);

	%obj = new ScriptObject(dsColorSetManagerSO);
	%colorSets = new GuiTextListCtrl();
	%obj.colorSets = %colorSets;

	%pattern = "Add-Ons/Colorset_*";
	%path = findFirstFile(%pattern);
	%prefixLen = strlen(%directory);

	%file = new FileObject();

	while (isFile(%path))
	{
		%name = strlwr(fileName(%path));
		if (%name !$= "colorset.txt" || !%file.openForRead(%path))
		{
			%path = findNextFile(%pattern);
			continue;
		}

		%mod = getSubStr(%path, 17, 1e5);
		%mod = getSubStr(%mod, 0, strstr(%mod, "/"));

		%colorSet = new ScriptObject()
		{
			class = dsColorSetSO;
			name = %mod;
		};

		%obj.readColorSet(%file, %colorSet);

		%colorSets.addRow(%colorSet, %mod);

		%file.close();
		%path = findNextFile(%pattern);
	}

	// Detect default colorset.

	%path = "";
	if ($GameModeArg !$= "")
		%path = filePath($GameModeArg) @ "/colorSet.txt";
	if (!isFile(%path))
		%path = "config/server/colorSet.txt";

	if (isFile(%path) && %file.openForRead(%path))
	{
		%colorSet = new ScriptObject()
		{
			class = dsColorSetSO;
			name = "Custom";
		};

		%obj.readColorSet(%file, %colorSet);
		%file.close();

		%colorCount = %colorSet.colorCount;
		%divs = %colorSet.divs;

		%match = 0;
		%count = %colorSets.rowCount();
		for (%i = 0; %i < %count; %i++)
		{
			%otherColorSet = %colorSets.getRowId(%i);

			if (%otherColorSet.divs !$= %divs || %otherColorSet.colorCount != %colorCount)
				continue;

			%noMatch = 0;

			for (%j = 0; %j < %colorCount; %j++)
			{
				if (%colorSet.color[%j] !$= %otherColorSet.color[%j])
				{
					%noMatch = 1;
					break;
				}
			}

			if (!%noMatch)
			{
				%match = 1;
				%colorSet.delete();
				%obj.defaultColorSet = %otherColorSet;
				break;
			}
		}

		if (!%match)
		{
			%obj.defaultColorSet = %colorSet;
			%colorSets.addRow(%colorSet, "Custom");
		}
	}

	if (!isObject(%obj.defaultColorSet) && %colorSets.rowCount())
	{
		%defaultIdx = %colorSets.findTextIndex("Default");
		%obj.defaultColorSet = %colorSets.getRowId(%defaultIdx != -1 ? %defaultIdx : 0);
	}

	%colorSets.sort(0, 1);

	%file.delete();

	return %obj;
}

function dsColorSetManagerSO::findColorSet(%this, %name)
{
	%colorSets = %this.colorSets;
	%idx = %colorSets.findTextIndex(%name);

	return %idx == -1 ? 0 : %colorSets.getRowId(%idx);
}

function dsColorSetManagerSO::readColorSet(%this, %file, %colorSet)
{
	%c = -1;
	%d = 0;

	while (!%file.isEOF())
	{
		%line = %file.readLine();

		if ((%comment = strstr(%line, "/")) != -1)
			%line = getSubStr(%line, 0, %comment);

		%line = trim(%line);
		if (%line $= "")
			continue;

		if (strstr(%line, "DIV:") == 0)
		{
			if (%d++ > 16)
				break;

			%divs = %d == 1 ? (%c SPC trim(getSubStr(%line, 4, 1e5))) : (%divs TAB %c SPC trim(getSubStr(%line, 4, 1e5)));
		}
		else
		{
			%r = mAbs(getWord(%line, 0));
			%g = mAbs(getWord(%line, 1));
			%b = mAbs(getWord(%line, 2));
			%a = mAbs(getWord(%line, 3));

			if (%r > 1 || %g > 1 || %b > 1 || %a > 1)
				%colorSet.color[%c++] = getColorF(%r SPC %g SPC %b SPC getMax(%a, 1));
			else
				%colorSet.color[%c++] = %r SPC %g SPC %b SPC getMax(%a, 1 / 255);
		}
	}

	%colorSet.colorCount = %c++;
	%colorSet.divCount = %d;
	%colorSet.divs = %divs;
}

function dsColorSetManagerSO::makeSprayCanGeneric(%this)
{
	for (%i = 0; %i < 64; %i++)
	{
		%explosionParticle = nameToID("color" @ %i @ "PaintExplosionParticle");
		if (!isObject(%explosionParticle))
			break;

		%explosionParticle.colors[0] = rainbowPaintExplosionParticle.colors[0];
		%explosionParticle.colors[1] = rainbowPaintExplosionParticle.colors[1];
		%explosionParticle.colors[2] = rainbowPaintExplosionParticle.colors[2];
		%explosionParticle.colors[3] = rainbowPaintExplosionParticle.colors[3];
		%explosionParticle.useInvAlpha = 0;

		%dropletParticle = nameToID("color" @ %i @ "PaintDropletParticle");
		%dropletParticle.colors[0] = rainbowPaintDropletParticle.colors[0];
		%dropletParticle.colors[1] = rainbowPaintDropletParticle.colors[1];
		%dropletParticle.colors[2] = rainbowPaintDropletParticle.colors[2];
		%dropletParticle.colors[3] = rainbowPaintDropletParticle.colors[3];
		%dropletParticle.useInvAlpha = 0;

		%image = nameToID("color" @ %i @ "SprayCanImage");
		%image.shapeFile = "base/data/shapes/spraycan.dts";
		%image.colorShiftColor = chromeSprayCanImage.colorShiftColor;
	}

	color0PaintExplosionEmitter.useEmitterColors = 1;
}

function dsColorSetSO::apply(%this)
{
	if (dsColorSetManagerSO.lastColorSet == %this)
		return;

	%colorCount = %this.colorCount;
	%divCount = %this.divCount;
	%divs = %this.divs;

	for (%i = 0; %i < %colorCount; %i++)
		setColorTable(%i, %this.color[%i]);

	for (%i = %colorCount; %i < 64; %i++)
		setColorTable(%i, "1 0 1 0");

	for (%i = 0; %i < %divCount; %i++)
	{
		%entry = getField(%divs, %i);
		setSprayCanDivision(%i, %entry | 0, removeWord(%entry, 0));
	}
	for (%i = %divCount; %i < 16; %i++)
		setSprayCanDivision(%i, 0, "");

	dsColorSetManagerSO.lastColorSet = %this;
}

function serverCmdlistColorSets(%client)
{
	%colorSets = dsColorSetManagerSO.colorSets;
	%count = %colorSets.rowCount();

	messageClient(%client, '', "\c2Color Sets:");

	%i = 0;
	while (%i < %count)
	{
		%colorSet = %colorSets.getRowText(%i);
		messageClient(%client, '', "  \c6" @ %i++ @ ": \c3" @ %colorSet);
	}
}

function serverCmdsetColorSet(%client, %input)
{
	if (!%client.isAdmin)
	{
		%miniGame = %client.miniGame;
		if (!isObject(%miniGame) || !%miniGame.buildSession || %miniGame.owner != %client)
			return;
	}

	%colorSets = dsColorSetManagerSO.colorSets;
	%count = %colorSets.rowCount();

	if (!(%input | 0))
	{
		%input = strlwr(%input);

		for (%i = 0; %i < %count; %i++)
		{
			if (%input $= strlwr(%colorSets.getRowText(%i)))
			{
				%colorSet = %colorSets.getRowId(%i);
				break;
			}
		}
	}
	else if (%input >= 1 && %input <= %count)
	{
		%colorSet = %colorSets.getRowId(%input - 1);
	}

	if (!isObject(%colorSet))
	{
		messageClient(%client, '', "Could not find color set.");
		return;
	}

	%partition = %client.partition;
	%partition.setColorSet(%colorSet, 1);
}

}; // package Server_Dueling
