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
			Key = "OriginLimit",
			DisplayName = "Origin (Innate Boon) Limit",
			Description = "Determines how many Origins can be picked during Character Creation.",
			GroupName = "General",
			MinValue = 1,
			MaxValue = 100,
			StepSize = 1,
			DefaultValue = 15,
		},
	},
	SettingGroups = {
		[1] = "General",
	},
	UpdateLogList = { },
	ChangeConfig = false,
	HasArchive = false,
	NeedRestartWhenSettingChanged = false,
	Cover = nil,
	WorkshopCover = nil,
}
