// Copyright (c) Johnny Patterson

package Server_Dueling {

function dsWeaponManagerSO()
{
	if (isObject(dsWeaponManagerSO))
		return nameToID(dsWeaponManagerSO);

	return new ScriptObject(dsWeaponManagerSO)
	{
	};
}

function dsWeaponManagerSO::buildListFromDatablockGroup(%this)
{
	%count = DataBlockGroup.getCount();
	%list = new GuiTextListCtrl();

	for (%i = 0; %i < %count; %i++)
	{
		%db = DataBlockGroup.getObject(%i);

		if (%db.getClassName() !$= "ItemData" ||
			%db.uiName $= "" ||
			strlwr(%db.className) !$= "weapon" ||
			strlwr(%db.category) !$= "weapon")
		{
			continue;
		}

		if (!isObject(%db.maps))
			%db.maps = new SimSet(%db);

		%list.addRow(%db, %db.uiName);
	}

	%count = %list.rowCount();

	if (%count)
	{
		%list.sort(0, 1);

		%weapons = %list.getRowId(0);
		%defaultTargetState = 0;
		for (%i = 1; %i < %count; %i++)
		{
			%weapons = %weapons TAB %list.getRowId(%i);
			%defaultTargetState = %defaultTargetState TAB 0;
		}
		%this.list = %weapons;
		%this.defaultTargetState = %defaultTargetState;

		%weapons = 0 SPC %list.getRowText(0);
		for (%i = 1; %i < %count; %i++)
			%weapons = %weapons TAB %i SPC %list.getRowText(%i);
		%this.legacyDataServerList = %weapons;
	}
	else
	{
		%this.list = "";
	}

	%list.delete();
}

function dsWeaponManagerSO::findWeaponByUIName(%this, %name)
{
	// NOTE: This is slower than caching the ID's, but it avoids a possible string table exploit if clients spam random weapon names.

	%count = getFieldCount(%this.list);

	for (%i = 0; %i < %count; %i++)
	{
		%weapon = getField(%this.list, %i);

		if (%weapon.uiName $= %name)
			return %weapon;
	}

	return -1;
}

function dsWeaponManagerSO::getWeaponIndex(%this, %input)
{
	%count = getFieldCount(%this.list);

	for (%i = 0; %i < %count; %i++)
	{
		%weapon = getField(%this.list, %i);

		if (%weapon == %input)
			return %i;
	}

	return -1;
}

}; // package Server_Dueling
