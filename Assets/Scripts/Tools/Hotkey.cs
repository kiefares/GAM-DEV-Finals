using System;
using System.Collections.Generic;
using UnityEngine;

public class Hotkey
{
	private static readonly KeyCode[] keys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
	private HashSet<KeyCode> mainKeys;
	private HashSet<KeyCode> modifiers;
	// Whether no other keys should be pressed other than the indicated.
	private bool strict;

	public Hotkey()
	{
		Strict(false);
		MainKeys();
		Modifiers();
	}

	public Hotkey(KeyCode mainKey, params KeyCode[] modifiers)
	{
		Strict(false);
		MainKeys(mainKey);
		Modifiers(modifiers);
	}

	public Hotkey(KeyCode[] mainKeys, KeyCode[] modifiers)
	{
		Strict(false);
		MainKeys(mainKeys);
		Modifiers(modifiers);
	}

	public Hotkey(bool strict, KeyCode mainKey, params KeyCode[] modifiers)
	{
		Strict(strict);
		MainKeys(mainKey);
		Modifiers(modifiers);
	}

	public Hotkey(bool strict, KeyCode[] mainKeys, KeyCode[] modifiers)
	{
		Strict(strict);
		MainKeys(mainKeys);
		Modifiers(modifiers);
	}

	public Hotkey MainKeys(params KeyCode[] mainKeys)
	{
		this.mainKeys = new HashSet<KeyCode>();

		foreach (KeyCode key in mainKeys)
			this.mainKeys.Add(key);

		return this;
	}

	public Hotkey Modifiers(params KeyCode[] modifiers)
	{
		this.modifiers = new HashSet<KeyCode>();

		foreach (KeyCode key in modifiers)
			this.modifiers.Add(key);

		return this;
	}

	public Hotkey Strict(bool strict)
	{
		this.strict = strict;

		return this;
	}

	// If the one of the main keys were pressed at the current frame while the all modifers are held.
	public bool IsDown()
	{
		if (mainKeys?.Count == 0)
			return false;

		foreach (KeyCode key in mainKeys)
			if (Input.GetKeyDown(key))
				return CheckModifiers();

		return false;
	}

	public bool IsHeld()
	{
		if (mainKeys?.Count == 0)
			return false;

		foreach (KeyCode key in mainKeys)
			if (Input.GetKey(key))
				return CheckModifiers();

		return false;
	}

	private bool CheckModifiers()
	{
		foreach (KeyCode key in modifiers)
			if (!Input.GetKey(key))
				return false;

		if (strict)
			foreach (KeyCode key in keys)
				if (!modifiers.Contains(key) &&
					!mainKeys.Contains(key) &&
					Input.GetKey(key))
					return false;

		return true;
	}
}