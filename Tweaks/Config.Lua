return {
	Title = "Tweaks",
	Source = 0,
	FileId = 1,
	Version = "0.0.1.0",
	GameVersion = "1.0.56",
	Author = "nozwock",
	Description = "",
	FrontendPlugins = {
		[1] = "Tweaks.Frontend.dll",
	},
	BackendPlugins = {
		[1] = "Tweaks.Backend.dll",
	},
	TagList = {
		[1] = "Modifications",
	},
	Visibility = 0,
	DefaultSettings = {
		[1] = {
			SettingType = "Slider",
			Key = "BackgroundTraitLimit",
			DisplayName = "Innate Boons Limit",
			Description =
			"Determines how many Innate Boons can be picked during character creation.\nSet to 0 to disable.",
			GroupName = "New Game",
			MinValue = 0,
			MaxValue = 100,
			StepSize = 1,
			DefaultValue = 0,
		},
		[2] = {
			SettingType = "Slider",
			Key = "BonusMaxTeammate",
			DisplayName = "Bonus Teammate Limit",
			Description = "",
			GroupName = "Gameplay",
			MinValue = 0,
			MaxValue = 100,
			StepSize = 1,
			DefaultValue = 20,
		},
	},
	SettingGroups = {
		[1] = "New Game",
		[2] = "Gameplay",
	},
	UpdateLogList = {},
	ChangeConfig = false,
	HasArchive = false,
	NeedRestartWhenSettingChanged = false,
	Cover = nil,
	WorkshopCover = nil,
}
