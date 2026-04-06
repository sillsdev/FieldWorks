\barcodes bdefhijmrsuvyz
\dsc +
\incl \tx \txC \txY \0 \1s \1s. \2s \3s \3sm \3sf \3sn \3sp \1p \1pe \1pi \2p \3p \8Y
\luwfcs | The following single character definitions must be defined here in
| the multibyte section, because they consist of more than one byte
| in Unicode (acc Emails 2006-05-16 AB and Beth)
æ Æ ø Ø ɨ Ɨ                     | toneless vowels (apart from a-z, A-Z)
| Accented characters:
á Á é É í Í ó Ó ú Ú ǽ Ǽ ǿ Ǿ ɨ́ Ɨ́ | high tone vowels
ā Ā ē Ē ī Ī ō Ō ū Ū ǣ Ǣ ø̄ Ø̄ ɨ̄ Ɨ̄ | middle tone vowels
à À è È ì Ì ò Ò ù Ù æ̀ Æ̀ ø̀ Ø̀ ɨ̀ Ɨ̀ | low tone vowels
â Â ê Ê î Î ô Ô û Û æ̂ Æ̂ ø̂ Ø̂ ɨ̂ Ɨ̂ | falling tone vowels
a᷄ A᷄ e᷄ E᷄ i᷄ I᷄ o᷄ O᷄ u᷄ U᷄ æ᷄ Æ᷄ ø᷄ Ø᷄ ɨ᷄ Ɨ᷄ | rising tone vowels
\wfc - |hyphen for clitics (needed so that hyphens can be ignored in long verbs)
꞉ |vowel lengthening
0123456789 | Numbers
| ́ | high tone      (Causes Warning:
| ̄ | middle tone     Word formation character ''
| ̀ | low tone 	     already specified:
| ̂ | falling tone    will ignore it.)
| ᷄ | rising tone
\maxdecap 0
\noincap
\CSbegch
\co *** CONTROL FILE: KVGintx.ctl ***

\co Remove all Tone Marks (for ToneParse)

\ch "́" ""
 | Delete high tones

\ch "᷄" ""
 | Delete rising tones

\ch "̄" ""
 | Delete middle tones

\ch "̀" ""
 | Delete low tones

\ch "̂" ""
 | Delete falling tones

\co Remove Hyphens

\ch "-" ""
 | ignore hyphens everywhere

\CSend
