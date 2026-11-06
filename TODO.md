# Tweaks

- "Taiwu village merchants are capped at level 1."

  Maybe make the merchants higher tier relative to the player's PE level?

- Improve martial loadout system, having export/import functionality.

- Allow multiple spouses for Taiwu. But, the game is very much hard-coded around a single alive spouse system; see
  `CharacterDomain.GetAliveSpouse` which is used in various places to get the spouse character id.

  So, even if `RelationTypeHelper.AllowAddingHusbandOrWifeRelation` is patched to allow more spouses for Taiwu... it
  will mess up spouse interactions. Could have the `GetAliveSpouse` return a random alive spouse charId but I'm not
  sure how well this will work out. Need to look more into this.

  A similar mod https://steamcommunity.com/sharedfiles/filedetails/?id=3750326625 does not consider `GetAliveSpouse`.
  It even patches `GetAliveSpouse` to return invalid charId if the target is taiwu just because in another function
  (AllowAddingHusbandOrWifeRelation) they wanted `GetAliveSpouse` to not take into account the taiwu -\_-. Could've
  just checked if the spouseId is taiwu at the place instead of messing with GetAliveSpouse that is used in many
  places.
