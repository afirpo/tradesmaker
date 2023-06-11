# Trades Maker
A plain &amp; simple mod for Captain of Industry to let you configure new Quick Trades.

Since I'm a very lazy person, you can find more info inside the 'Trades.xml' file that you HAVE to keep inside of the Mod's folder.
In fact, that file shall contains any piece of data that belongs to the creation of the new Quick Trades, village by village, one pair of Sell-Buy products at time.

### WARNING
Removing existing Quick Trades could lead to problems when you'll try to load a savegame where these were used, because __your save wouldn't load__!

I'm not really sure that something could be done at this level, because it's a Serialization/Deserialization problem with a savegame file, but I'll still try to do some magic.

Adding new XML lines or altering any already defined line _should_ work, tho.
