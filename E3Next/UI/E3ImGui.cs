using E3Core.Processors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace MonoCore
{

	// /e3imgui UI extracted into dedicated partial class file
	public static class E3ImGUI
	{
		private static IMQ MQ = E3.MQ;
		public static ConcurrentQueue<string> MQCommandQueue = new ConcurrentQueue<string>();
		public static void ProcessMQCommands()
		{
			while (MQCommandQueue.Count > 0)
			{
				string command;
				if (MQCommandQueue.TryDequeue(out command))
				{
					E3.MQ.Cmd(command);
				}
			}
		}
		//
		//RobotoRegular
		//RobotoRegular (Large)
		//EQ Font 0 - Arial 10 Thin
		//EQ Font 1 - Arial 12 Thin
		//EQ Font 2 - Arial 14 Thin
		//EQ Font 3 - Arial 15 Thin
		//EQ Font 4 - Arial 16 Thin
		//EQ Font 5 - Arial 20 Bold
		//EQ Font 6 - Arial 24 Bold
		//EQ Font 7 - Arial 20
		//EQ Font 8 - Arial 24
		//EQ Font 9 - Courier New 14
		//EQ Font 10 - Arial 40 Bold
		//lucon.ttf, 13px
		public static Dictionary<String, string> FontList = new Dictionary<string, string>() { {"robo","RobotoRegular" }, {"robo-large", "RobotoRegular (Large)" }
		,{"arial-10", "EQ Font 0 - Arial 10 Thin" }
		,{"arial-12", "EQ Font 1 - Arial 12 Thin" }
		,{"arial-14", "EQ Font 2 - Arial 14 Thin" }
		,{"arial-15","EQ Font 3 - Arial 15 Thin" }
		,{"arial-16","EQ Font 4 - Arial 16 Thin" }
		,{"arial-20","EQ Font 7 - Arial 20" }
		,{"arial-24","EQ Font 8 - Arial 24" }
		,{"arial_bold-20","EQ Font 5 - Arial 20 Bold" }
		,{"arial_bold-24","EQ Font 6 - Arial 24 Bold" }
		,{"arial_bold-40","EQ Font 10 - Arial 40 Bold" }
		,{"courier_new-9","EQ Font 9 - Courier New 14" }
		,{"lucon-13","lucon.ttf, 13px" }};

		public static class FontAwesome
		{
			public const string FA500px = "\uf26e";
			public const string FAAddressBook = "\uf2b9";
			public const string FAAddressBookO = "\uf2ba";
			public const string FAAddressCard = "\uf2bb";
			public const string FAAddressCardO = "\uf2bc";
			public const string FAAdjust = "\uf042";
			public const string FAAdn = "\uf170";
			public const string FAAlignCenter = "\uf037";
			public const string FAAlignJustify = "\uf039";
			public const string FAAlignLeft = "\uf036";
			public const string FAAlignRight = "\uf038";
			public const string FAAmazon = "\uf270";
			public const string FAAmbulance = "\uf0f9";
			public const string FAAmericanSignLanguageInterpreting = "\uf2a3";
			public const string FAAnchor = "\uf13d";
			public const string FAAndroid = "\uf17b";
			public const string FAAngellist = "\uf209";
			public const string FAAngleDoubleDown = "\uf103";
			public const string FAAngleDoubleLeft = "\uf100";
			public const string FAAngleDoubleRight = "\uf101";
			public const string FAAngleDoubleUp = "\uf102";
			public const string FAAngleDown = "\uf107";
			public const string FAAngleLeft = "\uf104";
			public const string FAAngleRight = "\uf105";
			public const string FAAngleUp = "\uf106";
			public const string FAApple = "\uf179";
			public const string FAArchive = "\uf187";
			public const string FAAreaChart = "\uf1fe";
			public const string FAArrowCircleDown = "\uf0ab";
			public const string FAArrowCircleLeft = "\uf0a8";
			public const string FAArrowCircleODown = "\uf01a";
			public const string FAArrowCircleOLeft = "\uf190";
			public const string FAArrowCircleORight = "\uf18e";
			public const string FAArrowCircleOUp = "\uf01b";
			public const string FAArrowCircleRight = "\uf0a9";
			public const string FAArrowCircleUp = "\uf0aa";
			public const string FAArrowDown = "\uf063";
			public const string FAArrowLeft = "\uf060";
			public const string FAArrowRight = "\uf061";
			public const string FAArrowUp = "\uf062";
			public const string FAArrows = "\uf047";
			public const string FAArrowsAlt = "\uf0b2";
			public const string FAArrowsH = "\uf07e";
			public const string FAArrowsV = "\uf07d";
			public const string FAAslInterpreting = "\uf2a3";
			public const string FAAssistiveListeningSystems = "\uf2a2";
			public const string FAAsterisk = "\uf069";
			public const string FAAt = "\uf1fa";
			public const string FAAudioDescription = "\uf29e";
			public const string FAAutomobile = "\uf1b9";
			public const string FABackward = "\uf04a";
			public const string FABalanceScale = "\uf24e";
			public const string FABan = "\uf05e";
			public const string FABandcamp = "\uf2d5";
			public const string FABank = "\uf19c";
			public const string FABarChart = "\uf080";
			public const string FABarChartO = "\uf080";
			public const string FABarcode = "\uf02a";
			public const string FABars = "\uf0c9";
			public const string FABath = "\uf2cd";
			public const string FABathtub = "\uf2cd";
			public const string FABattery = "\uf240";
			public const string FABattery0 = "\uf244";
			public const string FABattery1 = "\uf243";
			public const string FABattery2 = "\uf242";
			public const string FABattery3 = "\uf241";
			public const string FABattery4 = "\uf240";
			public const string FABatteryEmpty = "\uf244";
			public const string FABatteryFull = "\uf240";
			public const string FABatteryHalf = "\uf242";
			public const string FABatteryQuarter = "\uf243";
			public const string FABatteryThreeQuarters = "\uf241";
			public const string FABed = "\uf236";
			public const string FABeer = "\uf0fc";
			public const string FABehance = "\uf1b4";
			public const string FABehanceSquare = "\uf1b5";
			public const string FABell = "\uf0f3";
			public const string FABellO = "\uf0a2";
			public const string FABellSlash = "\uf1f6";
			public const string FABellSlashO = "\uf1f7";
			public const string FABicycle = "\uf206";
			public const string FABinoculars = "\uf1e5";
			public const string FABirthdayCake = "\uf1fd";
			public const string FABitbucket = "\uf171";
			public const string FABitbucketSquare = "\uf172";
			public const string FABitcoin = "\uf15a";
			public const string FABlackTie = "\uf27e";
			public const string FABlind = "\uf29d";
			public const string FABluetooth = "\uf293";
			public const string FABluetoothB = "\uf294";
			public const string FABold = "\uf032";
			public const string FABolt = "\uf0e7";
			public const string FABomb = "\uf1e2";
			public const string FABook = "\uf02d";
			public const string FABookmark = "\uf02e";
			public const string FABookmarkO = "\uf097";
			public const string FABraille = "\uf2a1";
			public const string FABriefcase = "\uf0b1";
			public const string FABtc = "\uf15a";
			public const string FABug = "\uf188";
			public const string FABuilding = "\uf1ad";
			public const string FABuildingO = "\uf0f7";
			public const string FABullhorn = "\uf0a1";
			public const string FABullseye = "\uf140";
			public const string FABus = "\uf207";
			public const string FABuysellads = "\uf20d";
			public const string FACab = "\uf1ba";
			public const string FACalculator = "\uf1ec";
			public const string FACalendar = "\uf073";
			public const string FACalendarCheckO = "\uf274";
			public const string FACalendarMinusO = "\uf272";
			public const string FACalendarO = "\uf133";
			public const string FACalendarPlusO = "\uf271";
			public const string FACalendarTimesO = "\uf273";
			public const string FACamera = "\uf030";
			public const string FACameraRetro = "\uf083";
			public const string FACar = "\uf1b9";
			public const string FACaretDown = "\uf0d7";
			public const string FACaretLeft = "\uf0d9";
			public const string FACaretRight = "\uf0da";
			public const string FACaretSquareODown = "\uf150";
			public const string FACaretSquareOLeft = "\uf191";
			public const string FACaretSquareORight = "\uf152";
			public const string FACaretSquareOUp = "\uf151";
			public const string FACaretUp = "\uf0d8";
			public const string FACartArrowDown = "\uf218";
			public const string FACartPlus = "\uf217";
			public const string FACc = "\uf20a";
			public const string FACcAmex = "\uf1f3";
			public const string FACcDinersClub = "\uf24c";
			public const string FACcDiscover = "\uf1f2";
			public const string FACcJcb = "\uf24b";
			public const string FACcMastercard = "\uf1f1";
			public const string FACcPaypal = "\uf1f4";
			public const string FACcStripe = "\uf1f5";
			public const string FACcVisa = "\uf1f0";
			public const string FACertificate = "\uf0a3";
			public const string FAChain = "\uf0c1";
			public const string FAChainBroken = "\uf127";
			public const string FACheck = "\uf00c";
			public const string FACheckCircle = "\uf058";
			public const string FACheckCircleO = "\uf05d";
			public const string FACheckSquare = "\uf14a";
			public const string FACheckSquareO = "\uf046";
			public const string FAChevronCircleDown = "\uf13a";
			public const string FAChevronCircleLeft = "\uf137";
			public const string FAChevronCircleRight = "\uf138";
			public const string FAChevronCircleUp = "\uf139";
			public const string FAChevronDown = "\uf078";
			public const string FAChevronLeft = "\uf053";
			public const string FAChevronRight = "\uf054";
			public const string FAChevronUp = "\uf077";
			public const string FAChild = "\uf1ae";
			public const string FAChrome = "\uf268";
			public const string FACircle = "\uf111";
			public const string FACircleO = "\uf10c";
			public const string FACircleONotch = "\uf1ce";
			public const string FACircleThin = "\uf1db";
			public const string FAClipboard = "\uf0ea";
			public const string FAClockO = "\uf017";
			public const string FAClone = "\uf24d";
			public const string FAClose = "\uf00d";
			public const string FACloud = "\uf0c2";
			public const string FACloudDownload = "\uf0ed";
			public const string FACloudUpload = "\uf0ee";
			public const string FACny = "\uf157";
			public const string FACode = "\uf121";
			public const string FACodeFork = "\uf126";
			public const string FACodepen = "\uf1cb";
			public const string FACodiepie = "\uf284";
			public const string FACoffee = "\uf0f4";
			public const string FACog = "\uf013";
			public const string FACogs = "\uf085";
			public const string FAColumns = "\uf0db";
			public const string FAComment = "\uf075";
			public const string FACommentO = "\uf0e5";
			public const string FACommenting = "\uf27a";
			public const string FACommentingO = "\uf27b";
			public const string FAComments = "\uf086";
			public const string FACommentsO = "\uf0e6";
			public const string FACompass = "\uf14e";
			public const string FACompress = "\uf066";
			public const string FAConnectdevelop = "\uf20e";
			public const string FAContao = "\uf26d";
			public const string FACopy = "\uf0c5";
			public const string FACopyright = "\uf1f9";
			public const string FACreativeCommons = "\uf25e";
			public const string FACreditCard = "\uf09d";
			public const string FACreditCardAlt = "\uf283";
			public const string FACrop = "\uf125";
			public const string FACrosshairs = "\uf05b";
			public const string FACss3 = "\uf13c";
			public const string FACube = "\uf1b2";
			public const string FACubes = "\uf1b3";
			public const string FACut = "\uf0c4";
			public const string FACutlery = "\uf0f5";
			public const string FADashboard = "\uf0e4";
			public const string FADashcube = "\uf210";
			public const string FADatabase = "\uf1c0";
			public const string FADeaf = "\uf2a4";
			public const string FADeafness = "\uf2a4";
			public const string FADedent = "\uf03b";
			public const string FADelicious = "\uf1a5";
			public const string FADesktop = "\uf108";
			public const string FADeviantart = "\uf1bd";
			public const string FADiamond = "\uf219";
			public const string FADigg = "\uf1a6";
			public const string FADollar = "\uf155";
			public const string FADotCircleO = "\uf192";
			public const string FADownload = "\uf019";
			public const string FADribbble = "\uf17d";
			public const string FADriversLicense = "\uf2c2";
			public const string FADriversLicenseO = "\uf2c3";
			public const string FADropbox = "\uf16b";
			public const string FADrupal = "\uf1a9";
			public const string FAEdge = "\uf282";
			public const string FAEdit = "\uf044";
			public const string FAEercast = "\uf2da";
			public const string FAEject = "\uf052";
			public const string FAEllipsisH = "\uf141";
			public const string FAEllipsisV = "\uf142";
			public const string FAEmpire = "\uf1d1";
			public const string FAEnvelope = "\uf0e0";
			public const string FAEnvelopeO = "\uf003";
			public const string FAEnvelopeOpen = "\uf2b6";
			public const string FAEnvelopeOpenO = "\uf2b7";
			public const string FAEnvelopeSquare = "\uf199";
			public const string FAEnvira = "\uf299";
			public const string FAEraser = "\uf12d";
			public const string FAEtsy = "\uf2d7";
			public const string FAEur = "\uf153";
			public const string FAEuro = "\uf153";
			public const string FAExchange = "\uf0ec";
			public const string FAExclamation = "\uf12a";
			public const string FAExclamationCircle = "\uf06a";
			public const string FAExclamationTriangle = "\uf071";
			public const string FAExpand = "\uf065";
			public const string FAExpeditedssl = "\uf23e";
			public const string FAExternalLink = "\uf08e";
			public const string FAExternalLinkSquare = "\uf14c";
			public const string FAEye = "\uf06e";
			public const string FAEyeSlash = "\uf070";
			public const string FAEyedropper = "\uf1fb";
			public const string FAFa = "\uf2b4";
			public const string FAFacebook = "\uf09a";
			public const string FAFacebookF = "\uf09a";
			public const string FAFacebookOfficial = "\uf230";
			public const string FAFacebookSquare = "\uf082";
			public const string FAFastBackward = "\uf049";
			public const string FAFastForward = "\uf050";
			public const string FAFax = "\uf1ac";
			public const string FAFeed = "\uf09e";
			public const string FAFemale = "\uf182";
			public const string FAFighterJet = "\uf0fb";
			public const string FAFile = "\uf15b";
			public const string FAFileArchiveO = "\uf1c6";
			public const string FAFileAudioO = "\uf1c7";
			public const string FAFileCodeO = "\uf1c9";
			public const string FAFileExcelO = "\uf1c3";
			public const string FAFileImageO = "\uf1c5";
			public const string FAFileMovieO = "\uf1c8";
			public const string FAFileO = "\uf016";
			public const string FAFilePdfO = "\uf1c1";
			public const string FAFilePhotoO = "\uf1c5";
			public const string FAFilePictureO = "\uf1c5";
			public const string FAFilePowerpointO = "\uf1c4";
			public const string FAFileSoundO = "\uf1c7";
			public const string FAFileText = "\uf15c";
			public const string FAFileTextO = "\uf0f6";
			public const string FAFileVideoO = "\uf1c8";
			public const string FAFileWordO = "\uf1c2";
			public const string FAFileZipO = "\uf1c6";
			public const string FAFilesO = "\uf0c5";
			public const string FAFilm = "\uf008";
			public const string FAFilter = "\uf0b0";
			public const string FAFire = "\uf06d";
			public const string FAFireExtinguisher = "\uf134";
			public const string FAFirefox = "\uf269";
			public const string FAFirstOrder = "\uf2b0";
			public const string FAFlag = "\uf024";
			public const string FAFlagCheckered = "\uf11e";
			public const string FAFlagO = "\uf11d";
			public const string FAFlash = "\uf0e7";
			public const string FAFlask = "\uf0c3";
			public const string FAFlickr = "\uf16e";
			public const string FAFloppyO = "\uf0c7";
			public const string FAFolder = "\uf07b";
			public const string FAFolderO = "\uf114";
			public const string FAFolderOpen = "\uf07c";
			public const string FAFolderOpenO = "\uf115";
			public const string FAFont = "\uf031";
			public const string FAFontAwesome = "\uf2b4";
			public const string FAFonticons = "\uf280";
			public const string FAFortAwesome = "\uf286";
			public const string FAForumbee = "\uf211";
			public const string FAForward = "\uf04e";
			public const string FAFoursquare = "\uf180";
			public const string FAFreeCodeCamp = "\uf2c5";
			public const string FAFrownO = "\uf119";
			public const string FAFutbolO = "\uf1e3";
			public const string FAGamepad = "\uf11b";
			public const string FAGavel = "\uf0e3";
			public const string FAGbp = "\uf154";
			public const string FAGe = "\uf1d1";
			public const string FAGear = "\uf013";
			public const string FAGears = "\uf085";
			public const string FAGenderless = "\uf22d";
			public const string FAGetPocket = "\uf265";
			public const string FAGg = "\uf260";
			public const string FAGgCircle = "\uf261";
			public const string FAGift = "\uf06b";
			public const string FAGit = "\uf1d3";
			public const string FAGitSquare = "\uf1d2";
			public const string FAGithub = "\uf09b";
			public const string FAGithubAlt = "\uf113";
			public const string FAGithubSquare = "\uf092";
			public const string FAGitlab = "\uf296";
			public const string FAGittip = "\uf184";
			public const string FAGlass = "\uf000";
			public const string FAGlide = "\uf2a5";
			public const string FAGlideG = "\uf2a6";
			public const string FAGlobe = "\uf0ac";
			public const string FAGoogle = "\uf1a0";
			public const string FAGooglePlus = "\uf0d5";
			public const string FAGooglePlusCircle = "\uf2b3";
			public const string FAGooglePlusOfficial = "\uf2b3";
			public const string FAGooglePlusSquare = "\uf0d4";
			public const string FAGoogleWallet = "\uf1ee";
			public const string FAGraduationCap = "\uf19d";
			public const string FAGratipay = "\uf184";
			public const string FAGrav = "\uf2d6";
			public const string FAGroup = "\uf0c0";
			public const string FAHSquare = "\uf0fd";
			public const string FAHackerNews = "\uf1d4";
			public const string FAHandGrabO = "\uf255";
			public const string FAHandLizardO = "\uf258";
			public const string FAHandODown = "\uf0a7";
			public const string FAHandOLeft = "\uf0a5";
			public const string FAHandORight = "\uf0a4";
			public const string FAHandOUp = "\uf0a6";
			public const string FAHandPaperO = "\uf256";
			public const string FAHandPeaceO = "\uf25b";
			public const string FAHandPointerO = "\uf25a";
			public const string FAHandRockO = "\uf255";
			public const string FAHandScissorsO = "\uf257";
			public const string FAHandSpockO = "\uf259";
			public const string FAHandStopO = "\uf256";
			public const string FAHandshakeO = "\uf2b5";
			public const string FAHardOfHearing = "\uf2a4";
			public const string FAHashtag = "\uf292";
			public const string FAHddO = "\uf0a0";
			public const string FAHeader = "\uf1dc";
			public const string FAHeadphones = "\uf025";
			public const string FAHeart = "\uf004";
			public const string FAHeartO = "\uf08a";
			public const string FAHeartbeat = "\uf21e";
			public const string FAHistory = "\uf1da";
			public const string FAHome = "\uf015";
			public const string FAHospitalO = "\uf0f8";
			public const string FAHotel = "\uf236";
			public const string FAHourglass = "\uf254";
			public const string FAHourglass1 = "\uf251";
			public const string FAHourglass2 = "\uf252";
			public const string FAHourglass3 = "\uf253";
			public const string FAHourglassEnd = "\uf253";
			public const string FAHourglassHalf = "\uf252";
			public const string FAHourglassO = "\uf250";
			public const string FAHourglassStart = "\uf251";
			public const string FAHouzz = "\uf27c";
			public const string FAHtml5 = "\uf13b";
			public const string FAICursor = "\uf246";
			public const string FAIdBadge = "\uf2c1";
			public const string FAIdCard = "\uf2c2";
			public const string FAIdCardO = "\uf2c3";
			public const string FAIls = "\uf20b";
			public const string FAImage = "\uf03e";
			public const string FAImdb = "\uf2d8";
			public const string FAInbox = "\uf01c";
			public const string FAIndent = "\uf03c";
			public const string FAIndustry = "\uf275";
			public const string FAInfo = "\uf129";
			public const string FAInfoCircle = "\uf05a";
			public const string FAInr = "\uf156";
			public const string FAInstagram = "\uf16d";
			public const string FAInstitution = "\uf19c";
			public const string FAInternetExplorer = "\uf26b";
			public const string FAIntersex = "\uf224";
			public const string FAIoxhost = "\uf208";
			public const string FAItalic = "\uf033";
			public const string FAJoomla = "\uf1aa";
			public const string FAJpy = "\uf157";
			public const string FAJsfiddle = "\uf1cc";
			public const string FAKey = "\uf084";
			public const string FAKeyboardO = "\uf11c";
			public const string FAKrw = "\uf159";
			public const string FALanguage = "\uf1ab";
			public const string FALaptop = "\uf109";
			public const string FALastfm = "\uf202";
			public const string FALastfmSquare = "\uf203";
			public const string FALeaf = "\uf06c";
			public const string FALeanpub = "\uf212";
			public const string FALegal = "\uf0e3";
			public const string FALemonO = "\uf094";
			public const string FALevelDown = "\uf149";
			public const string FALevelUp = "\uf148";
			public const string FALifeBouy = "\uf1cd";
			public const string FALifeBuoy = "\uf1cd";
			public const string FALifeRing = "\uf1cd";
			public const string FALifeSaver = "\uf1cd";
			public const string FALightbulbO = "\uf0eb";
			public const string FALineChart = "\uf201";
			public const string FALink = "\uf0c1";
			public const string FALinkedin = "\uf0e1";
			public const string FALinkedinSquare = "\uf08c";
			public const string FALinode = "\uf2b8";
			public const string FALinux = "\uf17c";
			public const string FAList = "\uf03a";
			public const string FAListAlt = "\uf022";
			public const string FAListOl = "\uf0cb";
			public const string FAListUl = "\uf0ca";
			public const string FALocationArrow = "\uf124";
			public const string FALock = "\uf023";
			public const string FALongArrowDown = "\uf175";
			public const string FALongArrowLeft = "\uf177";
			public const string FALongArrowRight = "\uf178";
			public const string FALongArrowUp = "\uf176";
			public const string FALowVision = "\uf2a8";
			public const string FAMagic = "\uf0d0";
			public const string FAMagnet = "\uf076";
			public const string FAMailForward = "\uf064";
			public const string FAMailReply = "\uf112";
			public const string FAMailReplyAll = "\uf122";
			public const string FAMale = "\uf183";
			public const string FAMap = "\uf279";
			public const string FAMapMarker = "\uf041";
			public const string FAMapO = "\uf278";
			public const string FAMapPin = "\uf276";
			public const string FAMapSigns = "\uf277";
			public const string FAMars = "\uf222";
			public const string FAMarsDouble = "\uf227";
			public const string FAMarsStroke = "\uf229";
			public const string FAMarsStrokeH = "\uf22b";
			public const string FAMarsStrokeV = "\uf22a";
			public const string FAMaxcdn = "\uf136";
			public const string FAMeanpath = "\uf20c";
			public const string FAMedium = "\uf23a";
			public const string FAMedkit = "\uf0fa";
			public const string FAMeetup = "\uf2e0";
			public const string FAMehO = "\uf11a";
			public const string FAMercury = "\uf223";
			public const string FAMicrochip = "\uf2db";
			public const string FAMicrophone = "\uf130";
			public const string FAMicrophoneSlash = "\uf131";
			public const string FAMinus = "\uf068";
			public const string FAMinusCircle = "\uf056";
			public const string FAMinusSquare = "\uf146";
			public const string FAMinusSquareO = "\uf147";
			public const string FAMixcloud = "\uf289";
			public const string FAMobile = "\uf10b";
			public const string FAMobilePhone = "\uf10b";
			public const string FAModx = "\uf285";
			public const string FAMoney = "\uf0d6";
			public const string FAMoonO = "\uf186";
			public const string FAMortarBoard = "\uf19d";
			public const string FAMotorcycle = "\uf21c";
			public const string FAMousePointer = "\uf245";
			public const string FAMusic = "\uf001";
			public const string FANavicon = "\uf0c9";
			public const string FANeuter = "\uf22c";
			public const string FANewspaperO = "\uf1ea";
			public const string FAObjectGroup = "\uf247";
			public const string FAObjectUngroup = "\uf248";
			public const string FAOdnoklassniki = "\uf263";
			public const string FAOdnoklassnikiSquare = "\uf264";
			public const string FAOpencart = "\uf23d";
			public const string FAOpenid = "\uf19b";
			public const string FAOpera = "\uf26a";
			public const string FAOptinMonster = "\uf23c";
			public const string FAOutdent = "\uf03b";
			public const string FAPagelines = "\uf18c";
			public const string FAPaintBrush = "\uf1fc";
			public const string FAPaperPlane = "\uf1d8";
			public const string FAPaperPlaneO = "\uf1d9";
			public const string FAPaperclip = "\uf0c6";
			public const string FAParagraph = "\uf1dd";
			public const string FAPaste = "\uf0ea";
			public const string FAPause = "\uf04c";
			public const string FAPauseCircle = "\uf28b";
			public const string FAPauseCircleO = "\uf28c";
			public const string FAPaw = "\uf1b0";
			public const string FAPaypal = "\uf1ed";
			public const string FAPencil = "\uf040";
			public const string FAPencilSquare = "\uf14b";
			public const string FAPencilSquareO = "\uf044";
			public const string FAPercent = "\uf295";
			public const string FAPhone = "\uf095";
			public const string FAPhoneSquare = "\uf098";
			public const string FAPhoto = "\uf03e";
			public const string FAPictureO = "\uf03e";
			public const string FAPieChart = "\uf200";
			public const string FAPiedPiper = "\uf2ae";
			public const string FAPiedPiperAlt = "\uf1a8";
			public const string FAPiedPiperPp = "\uf1a7";
			public const string FAPinterest = "\uf0d2";
			public const string FAPinterestP = "\uf231";
			public const string FAPinterestSquare = "\uf0d3";
			public const string FAPlane = "\uf072";
			public const string FAPlay = "\uf04b";
			public const string FAPlayCircle = "\uf144";
			public const string FAPlayCircleO = "\uf01d";
			public const string FAPlug = "\uf1e6";
			public const string FAPlus = "\uf067";
			public const string FAPlusCircle = "\uf055";
			public const string FAPlusSquare = "\uf0fe";
			public const string FAPlusSquareO = "\uf196";
			public const string FAPodcast = "\uf2ce";
			public const string FAPowerOff = "\uf011";
			public const string FAPrint = "\uf02f";
			public const string FAProductHunt = "\uf288";
			public const string FAPuzzlePiece = "\uf12e";
			public const string FAQq = "\uf1d6";
			public const string FAQrcode = "\uf029";
			public const string FAQuestion = "\uf128";
			public const string FAQuestionCircle = "\uf059";
			public const string FAQuestionCircleO = "\uf29c";
			public const string FAQuora = "\uf2c4";
			public const string FAQuoteLeft = "\uf10d";
			public const string FAQuoteRight = "\uf10e";
			public const string FARa = "\uf1d0";
			public const string FARandom = "\uf074";
			public const string FARavelry = "\uf2d9";
			public const string FARebel = "\uf1d0";
			public const string FARecycle = "\uf1b8";
			public const string FAReddit = "\uf1a1";
			public const string FARedditAlien = "\uf281";
			public const string FARedditSquare = "\uf1a2";
			public const string FARefresh = "\uf021";
			public const string FARegistered = "\uf25d";
			public const string FARemove = "\uf00d";
			public const string FARenren = "\uf18b";
			public const string FAReorder = "\uf0c9";
			public const string FARepeat = "\uf01e";
			public const string FAReply = "\uf112";
			public const string FAReplyAll = "\uf122";
			public const string FAResistance = "\uf1d0";
			public const string FARetweet = "\uf079";
			public const string FARmb = "\uf157";
			public const string FARoad = "\uf018";
			public const string FARocket = "\uf135";
			public const string FARotateLeft = "\uf0e2";
			public const string FARotateRight = "\uf01e";
			public const string FARouble = "\uf158";
			public const string FARss = "\uf09e";
			public const string FARssSquare = "\uf143";
			public const string FARub = "\uf158";
			public const string FARuble = "\uf158";
			public const string FARupee = "\uf156";
			public const string FAS15 = "\uf2cd";
			public const string FASafari = "\uf267";
			public const string FASave = "\uf0c7";
			public const string FAScissors = "\uf0c4";
			public const string FAScribd = "\uf28a";
			public const string FASearch = "\uf002";
			public const string FASearchMinus = "\uf010";
			public const string FASearchPlus = "\uf00e";
			public const string FASellsy = "\uf213";
			public const string FASend = "\uf1d8";
			public const string FASendO = "\uf1d9";
			public const string FAServer = "\uf233";
			public const string FAShare = "\uf064";
			public const string FAShareAlt = "\uf1e0";
			public const string FAShareAltSquare = "\uf1e1";
			public const string FAShareSquare = "\uf14d";
			public const string FAShareSquareO = "\uf045";
			public const string FAShekel = "\uf20b";
			public const string FASheqel = "\uf20b";
			public const string FAShield = "\uf132";
			public const string FAShip = "\uf21a";
			public const string FAShirtsinbulk = "\uf214";
			public const string FAShoppingBag = "\uf290";
			public const string FAShoppingBasket = "\uf291";
			public const string FAShoppingCart = "\uf07a";
			public const string FAShower = "\uf2cc";
			public const string FASignIn = "\uf090";
			public const string FASignLanguage = "\uf2a7";
			public const string FASignOut = "\uf08b";
			public const string FASignal = "\uf012";
			public const string FASigning = "\uf2a7";
			public const string FASimplybuilt = "\uf215";
			public const string FASitemap = "\uf0e8";
			public const string FASkyatlas = "\uf216";
			public const string FASkype = "\uf17e";
			public const string FASlack = "\uf198";
			public const string FASliders = "\uf1de";
			public const string FASlideshare = "\uf1e7";
			public const string FASmileO = "\uf118";
			public const string FASnapchat = "\uf2ab";
			public const string FASnapchatGhost = "\uf2ac";
			public const string FASnapchatSquare = "\uf2ad";
			public const string FASnowflakeO = "\uf2dc";
			public const string FASoccerBallO = "\uf1e3";
			public const string FASort = "\uf0dc";
			public const string FASortAlphaAsc = "\uf15d";
			public const string FASortAlphaDesc = "\uf15e";
			public const string FASortAmountAsc = "\uf160";
			public const string FASortAmountDesc = "\uf161";
			public const string FASortAsc = "\uf0de";
			public const string FASortDesc = "\uf0dd";
			public const string FASortDown = "\uf0dd";
			public const string FASortNumericAsc = "\uf162";
			public const string FASortNumericDesc = "\uf163";
			public const string FASortUp = "\uf0de";
			public const string FASoundcloud = "\uf1be";
			public const string FASpaceShuttle = "\uf197";
			public const string FASpinner = "\uf110";
			public const string FASpoon = "\uf1b1";
			public const string FASpotify = "\uf1bc";
			public const string FASquare = "\uf0c8";
			public const string FASquareO = "\uf096";
			public const string FAStackExchange = "\uf18d";
			public const string FAStackOverflow = "\uf16c";
			public const string FAStar = "\uf005";
			public const string FAStarHalf = "\uf089";
			public const string FAStarHalfEmpty = "\uf123";
			public const string FAStarHalfFull = "\uf123";
			public const string FAStarHalfO = "\uf123";
			public const string FAStarO = "\uf006";
			public const string FASteam = "\uf1b6";
			public const string FASteamSquare = "\uf1b7";
			public const string FAStepBackward = "\uf048";
			public const string FAStepForward = "\uf051";
			public const string FAStethoscope = "\uf0f1";
			public const string FAStickyNote = "\uf249";
			public const string FAStickyNoteO = "\uf24a";
			public const string FAStop = "\uf04d";
			public const string FAStopCircle = "\uf28d";
			public const string FAStopCircleO = "\uf28e";
			public const string FAStreetView = "\uf21d";
			public const string FAStrikethrough = "\uf0cc";
			public const string FAStumbleupon = "\uf1a4";
			public const string FAStumbleuponCircle = "\uf1a3";
			public const string FASubscript = "\uf12c";
			public const string FASubway = "\uf239";
			public const string FASuitcase = "\uf0f2";
			public const string FASunO = "\uf185";
			public const string FASuperpowers = "\uf2dd";
			public const string FASuperscript = "\uf12b";
			public const string FASupport = "\uf1cd";
			public const string FATable = "\uf0ce";
			public const string FATablet = "\uf10a";
			public const string FATachometer = "\uf0e4";
			public const string FATag = "\uf02b";
			public const string FATags = "\uf02c";
			public const string FATasks = "\uf0ae";
			public const string FATaxi = "\uf1ba";
			public const string FATelegram = "\uf2c6";
			public const string FATelevision = "\uf26c";
			public const string FATencentWeibo = "\uf1d5";
			public const string FATerminal = "\uf120";
			public const string FATextHeight = "\uf034";
			public const string FATextWidth = "\uf035";
			public const string FATh = "\uf00a";
			public const string FAThLarge = "\uf009";
			public const string FAThList = "\uf00b";
			public const string FAThemeisle = "\uf2b2";
			public const string FAThermometer = "\uf2c7";
			public const string FAThermometer0 = "\uf2cb";
			public const string FAThermometer1 = "\uf2ca";
			public const string FAThermometer2 = "\uf2c9";
			public const string FAThermometer3 = "\uf2c8";
			public const string FAThermometer4 = "\uf2c7";
			public const string FAThermometerEmpty = "\uf2cb";
			public const string FAThermometerFull = "\uf2c7";
			public const string FAThermometerHalf = "\uf2c9";
			public const string FAThermometerQuarter = "\uf2ca";
			public const string FAThermometerThreeQuarters = "\uf2c8";
			public const string FAThumbTack = "\uf08d";
			public const string FAThumbsDown = "\uf165";
			public const string FAThumbsODown = "\uf088";
			public const string FAThumbsOUp = "\uf087";
			public const string FAThumbsUp = "\uf164";
			public const string FATicket = "\uf145";
			public const string FATimes = "\uf00d";
			public const string FATimesCircle = "\uf057";
			public const string FATimesCircleO = "\uf05c";
			public const string FATimesRectangle = "\uf2d3";
			public const string FATimesRectangleO = "\uf2d4";
			public const string FATint = "\uf043";
			public const string FAToggleDown = "\uf150";
			public const string FAToggleLeft = "\uf191";
			public const string FAToggleOff = "\uf204";
			public const string FAToggleOn = "\uf205";
			public const string FAToggleRight = "\uf152";
			public const string FAToggleUp = "\uf151";
			public const string FATrademark = "\uf25c";
			public const string FATrain = "\uf238";
			public const string FATransgender = "\uf224";
			public const string FATransgenderAlt = "\uf225";
			public const string FATrash = "\uf1f8";
			public const string FATrashO = "\uf014";
			public const string FATree = "\uf1bb";
			public const string FATrello = "\uf181";
			public const string FATripadvisor = "\uf262";
			public const string FATrophy = "\uf091";
			public const string FATruck = "\uf0d1";
			public const string FATry = "\uf195";
			public const string FATty = "\uf1e4";
			public const string FATumblr = "\uf173";
			public const string FATumblrSquare = "\uf174";
			public const string FATurkishLira = "\uf195";
			public const string FATv = "\uf26c";
			public const string FATwitch = "\uf1e8";
			public const string FATwitter = "\uf099";
			public const string FATwitterSquare = "\uf081";
			public const string FAUmbrella = "\uf0e9";
			public const string FAUnderline = "\uf0cd";
			public const string FAUndo = "\uf0e2";
			public const string FAUniversalAccess = "\uf29a";
			public const string FAUniversity = "\uf19c";
			public const string FAUnlink = "\uf127";
			public const string FAUnlock = "\uf09c";
			public const string FAUnlockAlt = "\uf13e";
			public const string FAUnsorted = "\uf0dc";
			public const string FAUpload = "\uf093";
			public const string FAUsb = "\uf287";
			public const string FAUsd = "\uf155";
			public const string FAUser = "\uf007";
			public const string FAUserCircle = "\uf2bd";
			public const string FAUserCircleO = "\uf2be";
			public const string FAUserMd = "\uf0f0";
			public const string FAUserO = "\uf2c0";
			public const string FAUserPlus = "\uf234";
			public const string FAUserSecret = "\uf21b";
			public const string FAUserTimes = "\uf235";
			public const string FAUsers = "\uf0c0";
			public const string FAVcard = "\uf2bb";
			public const string FAVcardO = "\uf2bc";
			public const string FAVenus = "\uf221";
			public const string FAVenusDouble = "\uf226";
			public const string FAVenusMars = "\uf228";
			public const string FAViacoin = "\uf237";
			public const string FAViadeo = "\uf2a9";
			public const string FAViadeoSquare = "\uf2aa";
			public const string FAVideoCamera = "\uf03d";
			public const string FAVimeo = "\uf27d";
			public const string FAVimeoSquare = "\uf194";
			public const string FAVine = "\uf1ca";
			public const string FAVk = "\uf189";
			public const string FAVolumeControlPhone = "\uf2a0";
			public const string FAVolumeDown = "\uf027";
			public const string FAVolumeOff = "\uf026";
			public const string FAVolumeUp = "\uf028";
			public const string FAWarning = "\uf071";
			public const string FAWechat = "\uf1d7";
			public const string FAWeibo = "\uf18a";
			public const string FAWeixin = "\uf1d7";
			public const string FAWhatsapp = "\uf232";
			public const string FAWheelchair = "\uf193";
			public const string FAWheelchairAlt = "\uf29b";
			public const string FAWifi = "\uf1eb";
			public const string FAWikipediaW = "\uf266";
			public const string FAWindowClose = "\uf2d3";
			public const string FAWindowCloseO = "\uf2d4";
			public const string FAWindowMaximize = "\uf2d0";
			public const string FAWindowMinimize = "\uf2d1";
			public const string FAWindowRestore = "\uf2d2";
			public const string FAWindows = "\uf17a";
			public const string FAWon = "\uf159";
			public const string FAWordpress = "\uf19a";
			public const string FAWpbeginner = "\uf297";
			public const string FAWpexplorer = "\uf2de";
			public const string FAWpforms = "\uf298";
			public const string FAWrench = "\uf0ad";
			public const string FAXing = "\uf168";
			public const string FAXingSquare = "\uf169";
			public const string FAYCombinator = "\uf23b";
			public const string FAYCombinatorSquare = "\uf1d4";
			public const string FAYahoo = "\uf19e";
			public const string FAYc = "\uf23b";
			public const string FAYcSquare = "\uf1d4";
			public const string FAYelp = "\uf1e9";
			public const string FAYoast = "\uf2b1";
			public const string FAYoutube = "\uf167";
			public const string FAYoutubePlay = "\uf16a";
			public const string FAYoutubeSquare = "\uf166";
		}


		// Theme system with multiple themes
		public enum UITheme
		{
			DarkTeal,      // Original E3 theme
			DarkBlue,      // Blue accent variant
			DarkPurple,    // Purple accent variant
			DarkOrange,    // Orange accent variant
			DarkGreen      // Green accent variant
		}
		public enum ImGuiWindowFlags
		{
			ImGuiWindowFlags_None = 0,
			ImGuiWindowFlags_NoTitleBar = 1 << 0,   // Disable title-bar
			ImGuiWindowFlags_NoResize = 1 << 1,   // Disable user resizing with the lower-right grip
			ImGuiWindowFlags_NoMove = 1 << 2,   // Disable user moving the window
			ImGuiWindowFlags_NoScrollbar = 1 << 3,   // Disable scrollbars (window can still scroll with mouse or programmatically)
			ImGuiWindowFlags_NoScrollWithMouse = 1 << 4,   // Disable user vertically scrolling with mouse wheel. On child window, mouse wheel will be forwarded to the parent unless NoScrollbar is also set.
			ImGuiWindowFlags_NoCollapse = 1 << 5,   // Disable user collapsing window by double-clicking on it. Also referred to as "window menu button" within a docking node.
			ImGuiWindowFlags_AlwaysAutoResize = 1 << 6,   // Resize every window to its content every frame
			ImGuiWindowFlags_NoBackground = 1 << 7,   // Disable drawing background color (WindowBg, etc.) and outside border. Similar as using SetNextWindowBgAlpha(0.0f).
			ImGuiWindowFlags_NoSavedSettings = 1 << 8,   // Never load/save settings in .ini file
			ImGuiWindowFlags_NoMouseInputs = 1 << 9,   // Disable catching mouse, hovering test with pass through.
			ImGuiWindowFlags_MenuBar = 1 << 10,  // Has a menu-bar
			ImGuiWindowFlags_HorizontalScrollbar = 1 << 11,  // Allow horizontal scrollbar to appear (off by default). You may use SetNextWindowContentSize(ImVec2(width,0.0f)); prior to calling Begin() to specify width. Read code in imgui_demo in the "Horizontal Scrolling" section.
			ImGuiWindowFlags_NoFocusOnAppearing = 1 << 12,  // Disable taking focus when transitioning from hidden to visible state
			ImGuiWindowFlags_NoBringToFrontOnFocus = 1 << 13,  // Disable bringing window to front when taking focus (e.g. clicking on it or programmatically giving it focus)
			ImGuiWindowFlags_AlwaysVerticalScrollbar = 1 << 14,  // Always show vertical scrollbar (even if ContentSize.y < Size.y)
			ImGuiWindowFlags_AlwaysHorizontalScrollbar = 1 << 15,  // Always show horizontal scrollbar (even if ContentSize.x < Size.x)
			ImGuiWindowFlags_NoNavInputs = 1 << 16,  // No keyboard/gamepad navigation within the window
			ImGuiWindowFlags_NoNavFocus = 1 << 17,  // No focusing toward this window with keyboard/gamepad navigation (e.g. skipped by CTRL+TAB)
			ImGuiWindowFlags_UnsavedDocument = 1 << 18,  // Display a dot next to the title. When used in a tab/docking context, tab is selected when clicking the X + closure is not assumed (will wait for user to stop submitting the tab). Otherwise closure is assumed when pressing the X, so if you keep submitting the tab may reappear at end of tab bar.
			ImGuiWindowFlags_NoDocking = 1 << 19,  // Disable docking of this window

			ImGuiWindowFlags_NoNav = ImGuiWindowFlags_NoNavInputs | ImGuiWindowFlags_NoNavFocus,
			ImGuiWindowFlags_NoDecoration = ImGuiWindowFlags_NoTitleBar | ImGuiWindowFlags_NoResize | ImGuiWindowFlags_NoScrollbar | ImGuiWindowFlags_NoCollapse,
			ImGuiWindowFlags_NoInputs = ImGuiWindowFlags_NoMouseInputs | ImGuiWindowFlags_NoNavInputs | ImGuiWindowFlags_NoNavFocus,

			// [Internal]
			ImGuiWindowFlags_NavFlattened = 1 << 23,  // [BETA] Allow gamepad/keyboard navigation to cross over parent border to this child (only use on child that have no scrolling!)
			ImGuiWindowFlags_ChildWindow = 1 << 24,  // Don't use! For internal use by BeginChild()
			ImGuiWindowFlags_Tooltip = 1 << 25,  // Don't use! For internal use by BeginTooltip()
			ImGuiWindowFlags_Popup = 1 << 26,  // Don't use! For internal use by BeginPopup()
			ImGuiWindowFlags_Modal = 1 << 27,  // Don't use! For internal use by BeginPopupModal()
			ImGuiWindowFlags_ChildMenu = 1 << 28,  // Don't use! For internal use by BeginMenu()
			ImGuiWindowFlags_DockNodeHost = 1 << 29   // Don't use! For internal use by Begin()/NewFrame()

			// [Obsolete]
			//ImGuiWindowFlags_ResizeFromAnySide    = 1 << 17,  // --> Set io.ConfigWindowsResizeFromEdges=true and make sure mouse cursors are supported by backend (io.BackendFlags & ImGuiBackendFlags_HasMouseCursors)
		}
		public enum ImGuiMouseButton
		{
			Left = 0,
			Right = 1,
			Middle = 2,
			COUNT = 5
		};
		public enum ImGuiSelectableFlags
		{
			ImGuiSelectableFlags_None = 0,
			ImGuiSelectableFlags_NoAutoClosePopups = 1 << 0,   // Clicking this doesn't close parent popup window (overrides ImGuiItemFlags_AutoClosePopups)
			ImGuiSelectableFlags_SpanAllColumns = 1 << 1,   // Frame will span all columns of its container table (text will still fit in current column)
			ImGuiSelectableFlags_AllowDoubleClick = 1 << 2,   // Generate press events on double clicks too
			ImGuiSelectableFlags_Disabled = 1 << 3,   // Cannot be selected, display grayed out text
			ImGuiSelectableFlags_AllowOverlap = 1 << 4,   // (WIP) Hit testing to allow subsequent widgets to overlap this one
			ImGuiSelectableFlags_Highlight = 1 << 5,   // Make the item be displayed as if it is hovered
		};
		public enum ImGuiStyleVar
		{
			Alpha,
			DisabledAlpha,
			WindowPadding,          // ImVec2
			WindowRounding,         // float
			WindowBorderSize,       // float
			WindowMinSize,          // ImVec2
			WindowTitleAlign,       // ImVec2
			ChildRounding,          // float
			ChildBorderSize,        // float
			PopupRounding,          // float
			PopupBorderSize,        // float
			FramePadding,           // ImVec2
			FrameRounding,          // float
			FrameBorderSize,        // float
			ItemSpacing,            // ImVec2
			ItemInnerSpacing,       // ImVec2
			IndentSpacing,          // float
			CellPadding,            // ImVec2
			ScrollbarSize,          // float
			ScrollbarRounding,      // float
			GrabMinSize,            // float
			GrabRounding,           // float
			TabRounding,            // float
			ButtonTextAlign,        // ImVec2
			SelectableTextAlign     // ImVec2
		}
		public enum ImGuiChildFlags
		{
			None = 0,
			Borders = 1 << 0,   // Show an outer border and enable WindowPadding. (IMPORTANT: this is always == 1 == true for legacy reason)
			AlwaysUseWindowPadding = 1 << 1,   // Pad with style.WindowPadding even if no border are drawn (no padding by default for non-bordered child windows because it makes more sense)
			ResizeX = 1 << 2,   // Allow resize from right border (layout direction). Enable .ini saving (unless ImGuiWindowFlags_NoSavedSettings passed to window flags)
			ResizeY = 1 << 3,   // Allow resize from bottom border (layout direction). "
			AutoResizeX = 1 << 4,   // Enable auto-resizing width. Read "IMPORTANT: Size measurement" details above.
			AutoResizeY = 1 << 5,   // Enable auto-resizing height. Read "IMPORTANT: Size measurement" details above.
			AlwaysAutoResize = 1 << 6,   // Combined with AutoResizeX/AutoResizeY. Always measure size even when child is hidden, always return true, always disable clipping optimization! NOT RECOMMENDED.
			FrameStyle = 1 << 7,   // Style the child window like a framed item: use FrameBg, FrameRounding, FrameBorderSize, FramePadding instead of ChildBg, ChildRounding, ChildBorderSize, WindowPadding.
			NavFlattened = 1 << 8,   // [BETA] Share focus scope, allow keyboard/gamepad navigation to cross over parent border to this child or between sibling child windows.

		};
		public enum ImGuiCol
		{
			Text,
			TextDisabled,
			WindowBg,              // Background of normal windows
			ChildBg,               // Background of child windows
			PopupBg,               // Background of popups, menus, tooltips windows
			Border,
			BorderShadow,
			FrameBg,               // Background of checkbox, radio button, plot, slider, text input
			FrameBgHovered,
			FrameBgActive,
			TitleBg,               // Title bar
			TitleBgActive,         // Title bar when focused
			TitleBgCollapsed,      // Title bar when collapsed
			MenuBarBg,
			ScrollbarBg,
			ScrollbarGrab,
			ScrollbarGrabHovered,
			ScrollbarGrabActive,
			CheckMark,             // Checkbox tick and RadioButton circle
			SliderGrab,
			SliderGrabActive,
			Button,
			ButtonHovered,
			ButtonActive,
			Header,                // Header* colors are used for CollapsingHeader, TreeNode, Selectable, MenuItem
			HeaderHovered,
			HeaderActive,
			Separator,
			SeparatorHovered,
			SeparatorActive,
			ResizeGrip,            // Resize grip in lower-right and lower-left corners of windows.
			ResizeGripHovered,
			ResizeGripActive,
			TabHovered,            // Tab background, when hovered
			Tab,                   // Tab background, when tab-bar is focused & tab is unselected
			TabSelected,           // Tab background, when tab-bar is focused & tab is selected
			TabSelectedOverline,   // Tab horizontal overline, when tab-bar is focused & tab is selected
			TabDimmed,             // Tab background, when tab-bar is unfocused & tab is unselected
			TabDimmedSelected,     // Tab background, when tab-bar is unfocused & tab is selected
			TabDimmedSelectedOverline,//..horizontal overline, when tab-bar is unfocused & tab is selected
			DockingPreview,        // Preview overlay color when about to docking something
			DockingEmptyBg,        // Background color for empty node (e.g. CentralNode with no window docked into it)
			PlotLines,
			PlotLinesHovered,
			PlotHistogram,
			PlotHistogramHovered,
			TableHeaderBg,         // Table header background
			TableBorderStrong,     // Table outer and header borders (prefer using Alpha=1.0 here)
			TableBorderLight,      // Table inner borders (prefer using Alpha=1.0 here)
			TableRowBg,            // Table row background (even rows)
			TableRowBgAlt,         // Table row background (odd rows)
			TextLink,              // Hyperlink color
			TextSelectedBg,
			DragDropTarget,        // Rectangle highlighting a drop target
			NavCursor,             // Color of keyboard/gamepad navigation cursor/rectangle, when visible
			NavWindowingHighlight, // Highlight window when using CTRL+TAB
			NavWindowingDimBg,     // Darken/colorize entire screen behind the CTRL+TAB window list, when active
			ModalWindowDimBg,      // Darken/colorize entire screen behind a modal window, when one is active
			COUNT,
		}
		public enum ImGuiTableBgTarget
		{
			None = 0,
			RowBg0 = 1,        // Set row background color 0 (generally used for background, automatically set when ImGuiTableFlags_RowBg is used)
			RowBg1 = 2,        // Set row background color 1 (generally used for selection marking)
			CellBg = 3,        // Set cell background color (top-most color)
		};
		public enum ImGuiCond
		{
			None = 0,        // No condition (always set the variable), same as _Always
			Always = 1 << 0,   // No condition (always set the variable), same as _None
			Once = 1 << 1,   // Set the variable once per runtime session (only the first call will succeed)
			FirstUseEver = 1 << 2,   // Set the variable if the object/window has no persistently saved data (no entry in .ini file)
			Appearing = 1 << 3,   // Set the variable if the object/window is appearing after being hidden/inactive (or the first time)
		};
		public enum ImGuiTreeNodeFlags
		{
			ImGuiTreeNodeFlags_None = 0,
			ImGuiTreeNodeFlags_Selected = 1 << 0,   // Draw as selected
			ImGuiTreeNodeFlags_Framed = 1 << 1,   // Draw frame with background (e.g. for CollapsingHeader)
			ImGuiTreeNodeFlags_AllowItemOverlap = 1 << 2,   // Hit testing to allow subsequent widgets to overlap this one
			ImGuiTreeNodeFlags_NoTreePushOnOpen = 1 << 3,   // Don't do a TreePush() when open (e.g. for CollapsingHeader) = no extra indent nor pushing on ID stack
			ImGuiTreeNodeFlags_NoAutoOpenOnLog = 1 << 4,   // Don't automatically and temporarily open node when Logging is active (by default logging will automatically open tree nodes)
			ImGuiTreeNodeFlags_DefaultOpen = 1 << 5,   // Default node to be open
			ImGuiTreeNodeFlags_OpenOnDoubleClick = 1 << 6,   // Need double-click to open node
			ImGuiTreeNodeFlags_OpenOnArrow = 1 << 7,   // Only open when clicking on the arrow part. If ImGuiTreeNodeFlags_OpenOnDoubleClick is also set, single-click arrow or double-click all box to open.
			ImGuiTreeNodeFlags_Leaf = 1 << 8,   // No collapsing, no arrow (use as a convenience for leaf nodes)
			ImGuiTreeNodeFlags_Bullet = 1 << 9,   // Display a bullet instead of arrow
			ImGuiTreeNodeFlags_FramePadding = 1 << 10,  // Use FramePadding (even for an unframed text node) to vertically align text baseline to regular widget height. Equivalent to calling AlignTextToFramePadding().
			ImGuiTreeNodeFlags_SpanAvailWidth = 1 << 11,  // Extend hit box to the right-most edge, even if not framed. This is not the default in order to allow adding other items on the same line. In the future we may refactor the hit system to be front-to-back, allowing natural overlaps and then this can become the default.
			ImGuiTreeNodeFlags_SpanFullWidth = 1 << 12,  // Extend hit box to the left-most and right-most edges (bypass the indented area).
			ImGuiTreeNodeFlags_NavLeftJumpsBackHere = 1 << 13  // (WIP) Nav: left direction may move to this TreeNode() from any of its child (items submitted between TreeNode and TreePop)
		}

		public enum ImGuiTableFlags
		{
			ImGuiTableFlags_None = 0,
			ImGuiTableFlags_Resizable = 1 << 0,   // Enable resizing columns.
			ImGuiTableFlags_Reorderable = 1 << 1,   // Enable reordering columns in header row (need calling TableSetupColumn() + TableHeadersRow() to display headers)
			ImGuiTableFlags_Hideable = 1 << 2,   // Enable hiding/disabling columns in context menu.
			ImGuiTableFlags_Sortable = 1 << 3,   // Enable sorting. Call TableGetSortSpecs() to obtain sort specs. Also see ImGuiTableFlags_SortMulti and ImGuiTableFlags_SortTristate.
			ImGuiTableFlags_NoSavedSettings = 1 << 4,   // Disable persisting columns order, width and sort settings in the .ini file.
			ImGuiTableFlags_ContextMenuInBody = 1 << 5,   // Right-click on columns body/contents will display table context menu. By default it is available in TableHeadersRow().
														  // Decorations
			ImGuiTableFlags_RowBg = 1 << 6,   // Set each RowBg color with ImGuiCol_TableRowBg or ImGuiCol_TableRowBgAlt (equivalent of calling TableSetBgColor with ImGuiTableBgFlags_RowBg0 on each row)
			ImGuiTableFlags_BordersInnerH = 1 << 7,   // Draw horizontal borders between rows.
			ImGuiTableFlags_BordersOuterH = 1 << 8,   // Draw horizontal borders at the top and bottom.
			ImGuiTableFlags_BordersInnerV = 1 << 9,   // Draw vertical borders between columns.
			ImGuiTableFlags_BordersOuterV = 1 << 10,  // Draw vertical borders on the left and right sides.
			ImGuiTableFlags_BordersH = ImGuiTableFlags_BordersInnerH | ImGuiTableFlags_BordersOuterH, // Draw horizontal borders.
			ImGuiTableFlags_BordersV = ImGuiTableFlags_BordersInnerV | ImGuiTableFlags_BordersOuterV, // Draw vertical borders.
			ImGuiTableFlags_BordersInner = ImGuiTableFlags_BordersInnerV | ImGuiTableFlags_BordersInnerH, // Draw inner borders.
			ImGuiTableFlags_BordersOuter = ImGuiTableFlags_BordersOuterV | ImGuiTableFlags_BordersOuterH, // Draw outer borders.
			ImGuiTableFlags_Borders = ImGuiTableFlags_BordersInner | ImGuiTableFlags_BordersOuter,   // Draw all borders.
			ImGuiTableFlags_NoBordersInBody = 1 << 11,  // [ALPHA] Disable vertical borders in columns Body (borders will always appear in Headers). -> May move to style
			ImGuiTableFlags_NoBordersInBodyUntilResize = 1 << 12,  // [ALPHA] Disable vertical borders in columns Body until hovered for resize (borders will always appear in Headers). -> May move to style
																   // Sizing Policy (read above for defaults)
			ImGuiTableFlags_SizingFixedFit = 1 << 13,  // Columns default to _WidthFixed or _WidthAuto (if resizable or not resizable), matching contents width.
			ImGuiTableFlags_SizingFixedSame = 2 << 13,  // Columns default to _WidthFixed or _WidthAuto (if resizable or not resizable), matching the maximum contents width of all columns. Implicitly enable ImGuiTableFlags_NoKeepColumnsVisible.
			ImGuiTableFlags_SizingStretchProp = 3 << 13,  // Columns default to _WidthStretch with default weights proportional to each columns contents widths.
			ImGuiTableFlags_SizingStretchSame = 4 << 13,  // Columns default to _WidthStretch with default weights all equal, unless overridden by TableSetupColumn().
														  // Sizing Extra Options
			ImGuiTableFlags_NoHostExtendX = 1 << 16,  // Make outer width auto-fit to columns, overriding outer_size.x value. Only available when ScrollX/ScrollY are disabled and Stretch columns are not used.
			ImGuiTableFlags_NoHostExtendY = 1 << 17,  // Make outer height stop exactly at outer_size.y (prevent auto-extending table past the limit). Only available when ScrollX/ScrollY are disabled. Data below the limit will be clipped and not visible.
			ImGuiTableFlags_NoKeepColumnsVisible = 1 << 18,  // Disable keeping column always minimally visible when ScrollX is off and table gets too small. Not recommended if columns are resizable.
			ImGuiTableFlags_PreciseWidths = 1 << 19,  // Disable distributing remainder width to stretched columns (width allocation on a 100-wide table with 3 columns: Without this flag: 33,33,34. With this flag: 33,33,33). With larger number of columns, resizing will appear to be less smooth.
													  // Clipping
			ImGuiTableFlags_NoClip = 1 << 20,  // Disable clipping rectangle for every individual columns (reduce draw command count, items will be able to overflow into other columns). Generally incompatible with TableSetupScrollFreeze().
											   // Padding
			ImGuiTableFlags_PadOuterX = 1 << 21,  // Default if BordersOuterV is on. Enable outer-most padding. Generally desirable if you have headers.
			ImGuiTableFlags_NoPadOuterX = 1 << 22,  // Default if BordersOuterV is off. Disable outer-most padding.
			ImGuiTableFlags_NoPadInnerX = 1 << 23,  // Disable inner padding between columns (double inner padding if BordersOuterV is on, single inner padding if BordersOuterV is off).
													// Scrolling
			ImGuiTableFlags_ScrollX = 1 << 24,  // Enable horizontal scrolling. Require 'outer_size' parameter of BeginTable() to specify the container size. Changes default sizing policy. Because this creates a child window, ScrollY is currently generally recommended when using ScrollX.
			ImGuiTableFlags_ScrollY = 1 << 25,  // Enable vertical scrolling. Require 'outer_size' parameter of BeginTable() to specify the container size.
												// Sorting
			ImGuiTableFlags_SortMulti = 1 << 26,  // Hold shift when clicking headers to sort on multiple column. TableGetSortSpecs() may return specs where (SpecsCount > 1).
			ImGuiTableFlags_SortTristate = 1 << 27,  // Allow no sorting, disable default sorting. TableGetSortSpecs() may return specs where (SpecsCount == 0).
		}

		public enum ImGuiTableColumnFlags
		{
			ImGuiTableColumnFlags_None = 0,
			ImGuiTableColumnFlags_Disabled = 1 << 0,   // Overriding/master disable flag: hide column, won't show in context menu (unlike calling TableSetColumnEnabled() which manipulates the user accessible state)
			ImGuiTableColumnFlags_DefaultHide = 1 << 1,   // Default as a hidden/disabled column.
			ImGuiTableColumnFlags_DefaultSort = 1 << 2,   // Default as a sorting column.
			ImGuiTableColumnFlags_WidthStretch = 1 << 3,   // Column will stretch. Preferable with horizontal scrolling disabled (default if table sizing policy is _SizingStretchSame or _SizingStretchProp).
			ImGuiTableColumnFlags_WidthFixed = 1 << 4,   // Column will not stretch. Preferable with horizontal scrolling enabled (default if table sizing policy is _SizingFixedFit and table is resizable).
			ImGuiTableColumnFlags_NoResize = 1 << 5,   // Disable manual resizing.
			ImGuiTableColumnFlags_NoReorder = 1 << 6,   // Disable manual reordering this column, this will also prevent other columns from crossing over this column.
			ImGuiTableColumnFlags_NoHide = 1 << 7,   // Disable ability to hide/disable this column.
			ImGuiTableColumnFlags_NoClip = 1 << 8,   // Disable clipping for this column (all NoClip columns will render in a same draw command).
			ImGuiTableColumnFlags_NoSort = 1 << 9,   // Disable ability to sort on this field (even if ImGuiTableFlags_Sortable is set on the table).
			ImGuiTableColumnFlags_NoSortAscending = 1 << 10,  // Disable ability to sort in the ascending direction.
			ImGuiTableColumnFlags_NoSortDescending = 1 << 11,  // Disable ability to sort in the descending direction.
			ImGuiTableColumnFlags_NoHeaderLabel = 1 << 12,  // TableHeadersRow() will not submit label for this column. Convenient for some small columns. Name will still appear in context menu.
			ImGuiTableColumnFlags_NoHeaderWidth = 1 << 13,  // Disable header text width contribution to automatic column width.
			ImGuiTableColumnFlags_PreferSortAscending = 1 << 14,  // Make the initial sort direction Ascending when first sorting on this column (default).
			ImGuiTableColumnFlags_PreferSortDescending = 1 << 15,  // Make the initial sort direction Descending when first sorting on this column.
			ImGuiTableColumnFlags_IndentEnable = 1 << 16,  // Use current Indent value when entering cell (default for column 0).
			ImGuiTableColumnFlags_IndentDisable = 1 << 17,  // Ignore current Indent value when entering cell (default for columns > 0). Indentation changes _within_ the cell will still be honored.
		}

		public static uint GetColor(uint r, uint g, uint b, uint a)
		{
			return (a << 24) | (b << 16) | (g << 8) | r;
		}


		public static UITheme _currentTheme = UITheme.DarkTeal;

		private static readonly int _themePushCount = 27;
		// Rounding settings
		public static float _rounding = 6.0f;
		public static string _roundingBuf = string.Empty; // UI buffer for editing rounding
		public static int _roundingVersion = 0; // bump to force InputText to refresh its content
		public static readonly int _roundingPushCount = 7; // Window, Child, Popup, Frame, Grab, Tab, Scrollbar



		#region IMGUI
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_Begin(string name, int flags);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_Begin_OpenFlagSet(string name, bool value);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_Begin_OpenFlagGet(string name);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_Button(string name);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_ButtonEx(string name, float width, float height);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_Text(string text);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_Separator();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_SameLine();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_SameLineEx(float offsetFromStartX, float spacing);
		public static void imgui_SameLine(float offsetFromStartX)
	=> imgui_SameLineEx(offsetFromStartX, -1f);

		public static void imgui_SameLine(float offsetFromStartX, float spacing)
			=> imgui_SameLineEx(offsetFromStartX, spacing);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_Checkbox(string name, bool defaultValue);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_Checkbox_Get(string id);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_Checkbox_Clear(string id);


		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_BeginTabBar(string name);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_EndTabBar();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_BeginTabItem(string label);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_EndTabItem();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_BeginChild(string id, float width, float height, int child_flags, int window_flags);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_EndChild();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_Selectable(string label, bool selected);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_Selectable_WithFlags(string label, bool selected,int flags);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetContentRegionAvailX();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetContentRegionAvailY();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetWindowContentRegionMinX();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetWindowContentRegionMinY();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetWindowContentRegionMaxX();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetWindowContentRegionMaxY();


		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_SetNextItemWidth(float width);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_BeginCombo(string label, string preview, int flags);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_EndCombo();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_RightAlignButton(string name);
		[MethodImpl(MethodImplOptions.InternalCall)]

		public extern static bool imgui_InputTextMultiline(string id, string initial, float width, float height);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_InputText_Clear(string id);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_InputInt_Clear(string id);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_InputInt(string id, int initial, int steps, int faststeps);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static int imgui_InputInt_Get(string id);


		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_InputText(string id, string initial);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static string imgui_InputText_Get(string id);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_End();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_BeginPopupContextItem(string id, int flags);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_BeginPopupContextWindow(string id, int flags);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_EndPopup();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_MenuItem(string label);
		// Tables
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_BeginTable(string id, int columns, int flags, float outerWidth, float outerHeight);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_BeginTableS(string id, int columns, int flags);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_EndTable();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_TableSetupColumn(string label, int flags, float initWidth);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_TableSetupColumn_Default(string label);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_TableHeadersRow();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_TableNextRow();

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_TableNextColumn();

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_TableSetColumnIndex(int index);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_PushID(int id);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_PushFont(string name);


		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_PopFont();


		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_PopID();

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_TextColored(float r, float g, float b, float a, string text);
		// Colors / styled text
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_TextUnformatted(string text);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_CalcTextSizeX(string text);


		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_PushStyleColor(int which, float r, float g, float b, float a);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_PopStyleColor(int count);
		// Style vars (rounding, padding, etc.)
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_PushStyleVarFloat(int which, float value);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_PushStyleVarVec2(int which, float x, float y);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_PopStyleVar(int count);
		// Tree nodes
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_TreeNode(string label);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_TreeNodeEx(string label, int flags);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_TreePop();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_CollapsingHeader(string label, int flags);
		// Tooltips and hover detection
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_IsItemHovered();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_BeginTooltip();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_EndTooltip();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_TextWrapped(string text);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_PushTextWrapPos(float wrapLocalPosX);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_PopTextWrapPos();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_IsMouseClicked(int button);
		// Image display
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_Image(IntPtr textureId, float width, float height);
		// Native spell icon drawing
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_DrawSpellIconByIconIndex(int iconIndex, float size);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_DrawSpellIconBySpellID(int spellId, float size);
		// Drawing functions for custom backgrounds
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetCursorPosY();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetCursorScreenPosX();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetCursorScreenPosY();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetTextLineHeightWithSpacing();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetFrameHeight();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_GetWindowDrawList_AddRectFilled(float x1, float y1, float x2, float y2, uint color);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_GetWindowDrawList_AddText(float x, float y, uint color, string text);
		// Item rect + color helpers
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetItemRectMinX();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetItemRectMinY();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetItemRectMaxX();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetItemRectMaxY();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static uint imgui_GetColorU32(int imguiCol, float alphaMul);
		// Texture creation from raw data
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static IntPtr mq_CreateTextureFromData(byte[] data, int width, int height, int channels);
		[MethodImpl(MethodImplOptions.InternalCall)]

		public extern static void imgui_SetNextWindowBgAlpha(float alpha);
		[MethodImpl(MethodImplOptions.InternalCall)]

		public extern static void mq_DestroyTexture(IntPtr textureId);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_SetNextWindowSize(float width, float height);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_SetNextWindowPos(float x, float y, int flags, float xpiv, float ypiv);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetWindowPosX();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetWindowPosY();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetWindowSizeX();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetWindowSizeY();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_SetNextWindowFocus();

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_TableNextRowEx(int row_flags, float min_row_height);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetCursorPosX();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_SetCursorPosY(float y);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_SetCursorPosX(float x);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_SetNextWindowSizeWithCond(float width, float height, int cond);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_SetNextWindowSizeConstraints(float min_width, float min_height, float max_width, float max_height);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetWindowHeight();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetWindowWidth();

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_TableSetBgColor(int tablebgcolortarget, uint color, int currentcolumn);


		#endregion

		private static void PushCommonRounding()
		{
			// Apply consistent rounding across key style vars
			imgui_PushStyleVarFloat((int)ImGuiStyleVar.WindowRounding, _rounding);
			imgui_PushStyleVarFloat((int)ImGuiStyleVar.ChildRounding, _rounding);
			imgui_PushStyleVarFloat((int)ImGuiStyleVar.PopupRounding, _rounding);
			imgui_PushStyleVarFloat((int)ImGuiStyleVar.FrameRounding, _rounding);
			imgui_PushStyleVarFloat((int)ImGuiStyleVar.GrabRounding, Math.Max(3.0f, _rounding - 2.0f));
			imgui_PushStyleVarFloat((int)ImGuiStyleVar.TabRounding, _rounding);
			imgui_PushStyleVarFloat((int)ImGuiStyleVar.ScrollbarRounding, _rounding);
		}

		public static void PushCurrentTheme()
		{
			// Always push rounding first so it applies consistently regardless of selected theme
			PushCommonRounding();
			switch (_currentTheme)
			{
				case UITheme.DarkTeal:
					PushDarkTealTheme();
					break;
				case UITheme.DarkBlue:
					PushDarkBlueTheme();
					break;
				case UITheme.DarkPurple:
					PushDarkPurpleTheme();
					break;
				case UITheme.DarkOrange:
					PushDarkOrangeTheme();
					break;
				case UITheme.DarkGreen:
					PushDarkGreenTheme();
					break;
				default:
					PushDarkTealTheme();
					break;
			}
		}

		private static void PushDarkTealTheme()
		{
			// Backgrounds
			imgui_PushStyleColor((int)ImGuiCol.WindowBg, 0.13f, 0.13f, 0.14f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ChildBg, 0.11f, 0.11f, 0.12f, 1.0f);
			// Frames
			imgui_PushStyleColor((int)ImGuiCol.FrameBg, 0.17f, 0.18f, 0.20f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.FrameBgHovered, 0.20f, 0.21f, 0.23f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.FrameBgActive, 0.19f, 0.20f, 0.22f, 1.0f);
			// Buttons (teal accent)
			imgui_PushStyleColor((int)ImGuiCol.Button, 0.13f, 0.55f, 0.53f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ButtonHovered, 0.17f, 0.66f, 0.64f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ButtonActive, 0.12f, 0.48f, 0.47f, 1.0f);
			// Headers (used by tree nodes, selectable headers)
			imgui_PushStyleColor((int)ImGuiCol.Header, 0.12f, 0.50f, 0.49f, 0.55f);
			imgui_PushStyleColor((int)ImGuiCol.HeaderHovered, 0.16f, 0.62f, 0.60f, 0.80f);
			imgui_PushStyleColor((int)ImGuiCol.HeaderActive, 0.12f, 0.50f, 0.49f, 1.00f);
			// Tabs
			imgui_PushStyleColor((int)ImGuiCol.Tab, 0.11f, 0.48f, 0.46f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TabHovered, 0.16f, 0.62f, 0.60f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TabSelected, 0.13f, 0.55f, 0.53f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TabDimmed, 0.09f, 0.09f, 0.10f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TabDimmedSelected, 0.11f, 0.11f, 0.12f, 1.0f);
			// Sliders / checks
			imgui_PushStyleColor((int)ImGuiCol.SliderGrab, 0.29f, 0.79f, 0.76f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.SliderGrabActive, 0.36f, 0.86f, 0.80f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.CheckMark, 0.36f, 0.86f, 0.80f, 1.0f);
			// Titles
			imgui_PushStyleColor((int)ImGuiCol.TitleBg, 0.10f, 0.10f, 0.11f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TitleBgActive, 0.12f, 0.12f, 0.14f, 1.0f);
			// Separators
			imgui_PushStyleColor((int)ImGuiCol.Separator, 0.25f, 0.27f, 0.30f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.SeparatorHovered, 0.30f, 0.33f, 0.36f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.SeparatorActive, 0.21f, 0.60f, 0.60f, 1.0f);
			// Scrollbars
			imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrab, 0.28f, 0.30f, 0.32f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrabHovered, 0.32f, 0.34f, 0.36f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrabActive, 0.36f, 0.38f, 0.40f, 1.0f);
		}

		private static void PushDarkBlueTheme()
		{
			// Backgrounds
			imgui_PushStyleColor((int)ImGuiCol.WindowBg, 0.13f, 0.13f, 0.16f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ChildBg, 0.11f, 0.11f, 0.14f, 1.0f);
			// Frames
			imgui_PushStyleColor((int)ImGuiCol.FrameBg, 0.17f, 0.18f, 0.22f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.FrameBgHovered, 0.20f, 0.21f, 0.26f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.FrameBgActive, 0.19f, 0.20f, 0.24f, 1.0f);
			// Buttons (blue accent)
			imgui_PushStyleColor((int)ImGuiCol.Button, 0.26f, 0.39f, 0.98f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ButtonHovered, 0.32f, 0.45f, 1.0f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ButtonActive, 0.22f, 0.35f, 0.85f, 1.0f);
			// Headers
			imgui_PushStyleColor((int)ImGuiCol.Header, 0.26f, 0.39f, 0.98f, 0.55f);
			imgui_PushStyleColor((int)ImGuiCol.HeaderHovered, 0.32f, 0.45f, 1.0f, 0.80f);
			imgui_PushStyleColor((int)ImGuiCol.HeaderActive, 0.26f, 0.39f, 0.98f, 1.00f);
			// Tabs
			imgui_PushStyleColor((int)ImGuiCol.Tab, 0.22f, 0.35f, 0.85f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TabHovered, 0.32f, 0.45f, 1.0f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TabSelected, 0.26f, 0.39f, 0.98f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TabDimmed, 0.09f, 0.09f, 0.12f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TabDimmedSelected, 0.11f, 0.11f, 0.14f, 1.0f);
			// Sliders / checks
			imgui_PushStyleColor((int)ImGuiCol.SliderGrab, 0.32f, 0.45f, 1.0f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.SliderGrabActive, 0.38f, 0.51f, 1.0f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.CheckMark, 0.38f, 0.51f, 1.0f, 1.0f);
			// Titles
			imgui_PushStyleColor((int)ImGuiCol.TitleBg, 0.10f, 0.10f, 0.13f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TitleBgActive, 0.12f, 0.12f, 0.16f, 1.0f);
			// Separators
			imgui_PushStyleColor((int)ImGuiCol.Separator, 0.25f, 0.27f, 0.32f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.SeparatorHovered, 0.30f, 0.33f, 0.38f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.SeparatorActive, 0.26f, 0.39f, 0.98f, 1.0f);
			// Scrollbars
			imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrab, 0.28f, 0.30f, 0.34f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrabHovered, 0.32f, 0.34f, 0.38f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrabActive, 0.36f, 0.38f, 0.42f, 1.0f);
		}

		private static void PushDarkPurpleTheme()
		{
			// Backgrounds
			imgui_PushStyleColor((int)ImGuiCol.WindowBg, 0.15f, 0.12f, 0.16f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ChildBg, 0.13f, 0.10f, 0.14f, 1.0f);
			// Frames
			imgui_PushStyleColor((int)ImGuiCol.FrameBg, 0.19f, 0.16f, 0.22f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.FrameBgHovered, 0.22f, 0.19f, 0.26f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.FrameBgActive, 0.21f, 0.18f, 0.24f, 1.0f);
			// Buttons (purple accent)
			imgui_PushStyleColor((int)ImGuiCol.Button, 0.68f, 0.26f, 0.78f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ButtonHovered, 0.78f, 0.32f, 0.88f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ButtonActive, 0.58f, 0.22f, 0.68f, 1.0f);
			// Headers
			imgui_PushStyleColor((int)ImGuiCol.Header, 0.68f, 0.26f, 0.78f, 0.55f);
			imgui_PushStyleColor((int)ImGuiCol.HeaderHovered, 0.78f, 0.32f, 0.88f, 0.80f);
			imgui_PushStyleColor((int)ImGuiCol.HeaderActive, 0.68f, 0.26f, 0.78f, 1.00f);
			// Tabs
			imgui_PushStyleColor((int)ImGuiCol.Tab, 0.58f, 0.22f, 0.68f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TabHovered, 0.78f, 0.32f, 0.88f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TabSelected, 0.68f, 0.26f, 0.78f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TabDimmed, 0.11f, 0.08f, 0.12f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TabDimmedSelected, 0.13f, 0.10f, 0.14f, 1.0f);
			// Sliders / checks
			imgui_PushStyleColor((int)ImGuiCol.SliderGrab, 0.78f, 0.32f, 0.88f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.SliderGrabActive, 0.88f, 0.42f, 0.98f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.CheckMark, 0.88f, 0.42f, 0.98f, 1.0f);
			// Titles
			imgui_PushStyleColor((int)ImGuiCol.TitleBg, 0.12f, 0.09f, 0.13f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TitleBgActive, 0.14f, 0.11f, 0.16f, 1.0f);
			// Separators
			imgui_PushStyleColor((int)ImGuiCol.Separator, 0.27f, 0.24f, 0.32f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.SeparatorHovered, 0.32f, 0.29f, 0.38f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.SeparatorActive, 0.68f, 0.26f, 0.78f, 1.0f);
			// Scrollbars
			imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrab, 0.30f, 0.27f, 0.34f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrabHovered, 0.34f, 0.31f, 0.38f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrabActive, 0.38f, 0.35f, 0.42f, 1.0f);
		}

		private static void PushDarkOrangeTheme()
		{
			// Backgrounds
			imgui_PushStyleColor((int)ImGuiCol.WindowBg, 0.16f, 0.13f, 0.12f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ChildBg, 0.14f, 0.11f, 0.10f, 1.0f);
			// Frames
			imgui_PushStyleColor((int)ImGuiCol.FrameBg, 0.22f, 0.18f, 0.16f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.FrameBgHovered, 0.26f, 0.21f, 0.19f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.FrameBgActive, 0.24f, 0.20f, 0.18f, 1.0f);
			// Buttons (orange accent)
			imgui_PushStyleColor((int)ImGuiCol.Button, 0.98f, 0.55f, 0.26f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ButtonHovered, 1.0f, 0.65f, 0.32f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ButtonActive, 0.85f, 0.48f, 0.22f, 1.0f);
			// Headers
			imgui_PushStyleColor((int)ImGuiCol.Header, 0.98f, 0.55f, 0.26f, 0.55f);
			imgui_PushStyleColor((int)ImGuiCol.HeaderHovered, 1.0f, 0.65f, 0.32f, 0.80f);
			imgui_PushStyleColor((int)ImGuiCol.HeaderActive, 0.98f, 0.55f, 0.26f, 1.00f);
			// Tabs
			imgui_PushStyleColor((int)ImGuiCol.Tab, 0.85f, 0.48f, 0.22f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TabHovered, 1.0f, 0.65f, 0.32f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TabSelected, 0.98f, 0.55f, 0.26f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TabDimmed, 0.12f, 0.09f, 0.08f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TabDimmedSelected, 0.14f, 0.11f, 0.10f, 1.0f);
			// Sliders / checks
			imgui_PushStyleColor((int)ImGuiCol.SliderGrab, 1.0f, 0.65f, 0.32f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.SliderGrabActive, 1.0f, 0.75f, 0.42f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.CheckMark, 1.0f, 0.75f, 0.42f, 1.0f);
			// Titles
			imgui_PushStyleColor((int)ImGuiCol.TitleBg, 0.13f, 0.10f, 0.09f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TitleBgActive, 0.16f, 0.12f, 0.11f, 1.0f);
			// Separators
			imgui_PushStyleColor((int)ImGuiCol.Separator, 0.32f, 0.27f, 0.24f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.SeparatorHovered, 0.38f, 0.33f, 0.30f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.SeparatorActive, 0.98f, 0.55f, 0.26f, 1.0f);
			// Scrollbars
			imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrab, 0.34f, 0.30f, 0.28f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrabHovered, 0.38f, 0.34f, 0.32f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrabActive, 0.42f, 0.38f, 0.36f, 1.0f);
		}

		private static void PushDarkGreenTheme()
		{
			// Backgrounds
			imgui_PushStyleColor((int)ImGuiCol.WindowBg, 0.12f, 0.16f, 0.13f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ChildBg, 0.10f, 0.14f, 0.11f, 1.0f);
			// Frames
			imgui_PushStyleColor((int)ImGuiCol.FrameBg, 0.16f, 0.22f, 0.18f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.FrameBgHovered, 0.19f, 0.26f, 0.21f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.FrameBgActive, 0.18f, 0.24f, 0.20f, 1.0f);
			// Buttons (green accent)
			imgui_PushStyleColor((int)ImGuiCol.Button, 0.26f, 0.78f, 0.39f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ButtonHovered, 0.32f, 0.88f, 0.45f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ButtonActive, 0.22f, 0.68f, 0.35f, 1.0f);
			// Headers
			imgui_PushStyleColor((int)ImGuiCol.Header, 0.26f, 0.78f, 0.39f, 0.55f);
			imgui_PushStyleColor((int)ImGuiCol.HeaderHovered, 0.32f, 0.88f, 0.45f, 0.80f);
			imgui_PushStyleColor((int)ImGuiCol.HeaderActive, 0.26f, 0.78f, 0.39f, 1.00f);
			// Tabs
			imgui_PushStyleColor((int)ImGuiCol.Tab, 0.22f, 0.68f, 0.35f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TabHovered, 0.32f, 0.88f, 0.45f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TabSelected, 0.26f, 0.78f, 0.39f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TabDimmed, 0.08f, 0.12f, 0.09f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TabDimmedSelected, 0.10f, 0.14f, 0.11f, 1.0f);
			// Sliders / checks
			imgui_PushStyleColor((int)ImGuiCol.SliderGrab, 0.32f, 0.88f, 0.45f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.SliderGrabActive, 0.42f, 0.98f, 0.55f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.CheckMark, 0.42f, 0.98f, 0.55f, 1.0f);
			// Titles
			imgui_PushStyleColor((int)ImGuiCol.TitleBg, 0.09f, 0.13f, 0.10f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.TitleBgActive, 0.11f, 0.16f, 0.12f, 1.0f);
			// Separators
			imgui_PushStyleColor((int)ImGuiCol.Separator, 0.24f, 0.32f, 0.27f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.SeparatorHovered, 0.30f, 0.38f, 0.33f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.SeparatorActive, 0.26f, 0.78f, 0.39f, 1.0f);
			// Scrollbars
			imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrab, 0.27f, 0.34f, 0.30f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrabHovered, 0.31f, 0.38f, 0.34f, 1.0f);
			imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrabActive, 0.35f, 0.42f, 0.38f, 1.0f);
		}


		public static void PopCurrentTheme()
		{
			// Pop in reverse order: style vars then colors
			imgui_PopStyleVar(_roundingPushCount);
			imgui_PopStyleColor(_themePushCount);
		}


		public static float[] GetThemePreviewColor(UITheme theme)
		{
			switch (theme)
			{
				case UITheme.DarkTeal:
					return new float[] { 0.13f, 0.55f, 0.53f, 1.0f };
				case UITheme.DarkBlue:
					return new float[] { 0.26f, 0.39f, 0.98f, 1.0f };
				case UITheme.DarkPurple:
					return new float[] { 0.68f, 0.26f, 0.78f, 1.0f };
				case UITheme.DarkOrange:
					return new float[] { 0.98f, 0.55f, 0.26f, 1.0f };
				case UITheme.DarkGreen:
					return new float[] { 0.26f, 0.78f, 0.39f, 1.0f };
				default:
					return new float[] { 0.13f, 0.55f, 0.53f, 1.0f };
			}
		}

		public static string GetThemeDescription(UITheme theme)
		{
			switch (theme)
			{
				case UITheme.DarkTeal:
					return "The original E3Next dark theme with teal accents. Professional and easy on the eyes for long sessions.";
				case UITheme.DarkBlue:
					return "Dark theme with vibrant blue accents. Clean and modern appearance with good contrast.";
				case UITheme.DarkPurple:
					return "Dark theme with purple accents. Unique and stylish with a mystical feel.";
				case UITheme.DarkOrange:
					return "Dark theme with warm orange accents. Energetic and attention-grabbing design.";
				case UITheme.DarkGreen:
					return "Dark theme with green accents. Natural and calming, easy on the eyes.";
				default:
					return "Theme description not available.";
			}
		}
		/// <summary>
		/// Primary C++ entry point, calls the Invoke on all registered windows.
		/// </summary>
		public static void OnUpdateImGui()
		{
			if (Core.IsProcessing)
			{
				foreach (var pair in RegisteredWindows)
				{
					pair.Value.Invoke();
				}

			}
		}
		public static ConcurrentDictionary<string, Action> RegisteredWindows = new ConcurrentDictionary<string, Action>();

		//super simple registered method. no unregister, will add one if needed later.
		public static void RegisterWindow(string windowName, Action method, string description = "", [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
		{
			if (!RegisteredWindows.ContainsKey(windowName))
			{
				RegisteredWindows.TryAdd(windowName, method);
			}
		}
		public class ImGUICombo : IDisposable
		{
			bool IsOpen = false;
			public bool BeginCombo(string id, string preview, int window_flags = 0)
			{
				IsOpen = imgui_BeginCombo(id, preview, window_flags);
				return IsOpen;
			}
			#region objectPoolingStuff
			//private constructor, needs to be created so that you are forced to use the pool.
			private ImGUICombo()
			{

			}
			public static ImGUICombo Aquire()
			{
				ImGUICombo obj;
				if (!StaticObjectPool.TryPop<ImGUICombo>(out obj))
				{
					obj = new ImGUICombo();
				}

				return obj;
			}
			public void Dispose()
			{
				/*ImGui::End():
				Every ImGui::Begin() call must be paired with an 
				ImGui::End() call to properly close the window context and ensure correct rendering.*/
				if (IsOpen)
				{
					imgui_EndCombo();

				}
				IsOpen = false;
				StaticObjectPool.Push(this);
			}
			~ImGUICombo()
			{
				//DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
				//if this is called, it will cause the domain to hang in the GC when shuttind down
				//This is only here to warn you

			}

			#endregion
		}

		public class ImGUIWindow : IDisposable
		{

			public bool Begin(string id, int window_flags)
			{
				return imgui_Begin(id, window_flags);
			}
			#region objectPoolingStuff
			//private constructor, needs to be created so that you are forced to use the pool.
			private ImGUIWindow()
			{

			}
			public static ImGUIWindow Aquire()
			{
				ImGUIWindow obj;
				if (!StaticObjectPool.TryPop<ImGUIWindow>(out obj))
				{
					obj = new ImGUIWindow();
				}

				return obj;
			}
			public void Dispose()
			{
				/*ImGui::End():
				Every ImGui::Begin() call must be paired with an 
				ImGui::End() call to properly close the window context and ensure correct rendering.*/

				imgui_End();
				StaticObjectPool.Push(this);
			}
			~ImGUIWindow()
			{
				//DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
				//if this is called, it will cause the domain to hang in the GC when shuttind down
				//This is only here to warn you

			}

			#endregion
		}
		public class ImGUITabBar : IDisposable
		{
			public bool IsOpen = false;
			public bool BeginTabBar(string id)
			{
				IsOpen = imgui_BeginTabBar(id);
				return IsOpen;
			}
			#region objectPoolingStuff
			//private constructor, needs to be created so that you are forced to use the pool.
			private ImGUITabBar()
			{

			}
			public static ImGUITabBar Aquire()
			{
				ImGUITabBar obj;
				if (!StaticObjectPool.TryPop<ImGUITabBar>(out obj))
				{
					obj = new ImGUITabBar();
				}

				return obj;
			}
			public void Dispose()
			{
				//only call pop if the original call was set to open per IMGUI docs
				/*ImGui::TreePop():
				 * When TreeNodeEx returns true, you must call ImGui::TreePop() 
				 * after drawing all the child elements to correctly manage the tree's indentation and state
				 */
				if (IsOpen)
				{
					imgui_EndTabBar();
				}
				IsOpen = false;
				StaticObjectPool.Push(this);
			}
			~ImGUITabBar()
			{
				//DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
				//if this is called, it will cause the domain to hang in the GC when shuttind down
				//This is only here to warn you

			}

			#endregion
		}
		public class ImGUITabItem : IDisposable
		{
			public bool IsOpen = false;
			public bool BeginTabItem(string id)
			{
				IsOpen = imgui_BeginTabItem(id);
				return IsOpen;
			}
			#region objectPoolingStuff
			//private constructor, needs to be created so that you are forced to use the pool.
			private ImGUITabItem()
			{

			}
			public static ImGUITabItem Aquire()
			{
				ImGUITabItem obj;
				if (!StaticObjectPool.TryPop<ImGUITabItem>(out obj))
				{
					obj = new ImGUITabItem();
				}

				return obj;
			}
			public void Dispose()
			{

				if (IsOpen)
				{
					imgui_EndTabItem();
				}
				IsOpen = false;
				StaticObjectPool.Push(this);
			}
			~ImGUITabItem()
			{
				//DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
				//if this is called, it will cause the domain to hang in the GC when shuttind down
				//This is only here to warn you

			}

			#endregion
		}
		public class ImGUITree : IDisposable
		{
			public bool IsOpen = false;
			public bool TreeNodeEx(string id, int flags)
			{
				IsOpen = imgui_TreeNodeEx(id, flags);
				return IsOpen;
			}
			#region objectPoolingStuff
			//private constructor, needs to be created so that you are forced to use the pool.
			private ImGUITree()
			{

			}
			public static ImGUITree Aquire()
			{
				ImGUITree obj;
				if (!StaticObjectPool.TryPop<ImGUITree>(out obj))
				{
					obj = new ImGUITree();
				}

				return obj;
			}
			public void Dispose()
			{
				//only call pop if the original call was set to open per IMGUI docs
				/*ImGui::TreePop():
				 * When TreeNodeEx returns true, you must call ImGui::TreePop() 
				 * after drawing all the child elements to correctly manage the tree's indentation and state
				 */
				if (IsOpen)
				{
					imgui_TreePop();
				}
				IsOpen = false;
				StaticObjectPool.Push(this);
			}
			~ImGUITree()
			{
				//DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
				//if this is called, it will cause the domain to hang in the GC when shuttind down
				//This is only here to warn you

			}

			#endregion
		}
		public class ImGUIToolTip : IDisposable
		{
			public void BeginToolTip()
			{
			}
			#region objectPoolingStuff
			//private constructor, needs to be created so that you are forced to use the pool.
			private ImGUIToolTip()
			{

			}
			public static ImGUIToolTip Aquire()
			{
				ImGUIToolTip obj;
				if (!StaticObjectPool.TryPop<ImGUIToolTip>(out obj))
				{
					obj = new ImGUIToolTip();
				}
				//super simple method, just call it on the aquire so user doesn't have to call it themselves.
				imgui_BeginTooltip();
				return obj;
			}
			public void Dispose()
			{
				imgui_EndTooltip();
				StaticObjectPool.Push(this);
			}
			~ImGUIToolTip()
			{
				//DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
				//if this is called, it will cause the domain to hang in the GC when shuttind down
				//This is only here to warn you

			}

			#endregion
		}
		public class ImGUIPopUpContext : IDisposable
		{
			bool IsOpen = false;
			public bool BeginPopupContextItem(string id, int flags)
			{
				IsOpen = imgui_BeginPopupContextItem(id, flags);
				return IsOpen;
			}
			#region objectPoolingStuff
			//private constructor, needs to be created so that you are forced to use the pool.
			private ImGUIPopUpContext()
			{

			}
			public static ImGUIPopUpContext Aquire()
			{
				ImGUIPopUpContext obj;
				if (!StaticObjectPool.TryPop<ImGUIPopUpContext>(out obj))
				{
					obj = new ImGUIPopUpContext();
				}

				return obj;
			}
			public void Dispose()
			{
				/*Call ImGui::EndPopup() after drawing the contents to properly close the popup scope.
				 aka, only close if it was open to begin with*/

				if (IsOpen)
				{
					imgui_EndPopup();
				}
				IsOpen = false;
				StaticObjectPool.Push(this);
			}
			~ImGUIPopUpContext()
			{
				//DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
				//if this is called, it will cause the domain to hang in the GC when shuttind down
				//This is only here to warn you

			}

			#endregion
		}
		public class ImGUITable : IDisposable
		{
			public bool IsOpen = false;
			public bool BeginTable(string id, int columns, int flags, float outerWidth, float outerHeight)
			{
				IsOpen = imgui_BeginTable(id, columns, flags, outerWidth, outerHeight);
				return IsOpen;
			}
			#region objectPoolingStuff
			//private constructor, needs to be created so that you are forced to use the pool.
			private ImGUITable()
			{

			}
			public static ImGUITable Aquire()
			{
				ImGUITable obj;
				if (!StaticObjectPool.TryPop<ImGUITable>(out obj))
				{
					obj = new ImGUITable();
				}

				return obj;
			}
			public void Dispose()
			{
				/*
				Return Value:
				ImGui::BeginTable() returns true if the table is visible and active, and false otherwise. 
				You should only call ImGui::EndTable() if BeginTable() returns true.
				*/
				if (IsOpen)
				{
					imgui_EndTable();
				}
				IsOpen = false;
				StaticObjectPool.Push(this);
			}
			~ImGUITable()
			{
				//DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
				//if this is called, it will cause the domain to hang in the GC when shuttind down
				//This is only here to warn you

			}

			#endregion
		}
		public class ImGUIChild : IDisposable
		{

			public bool BeginChild(string id, float width, float height, int child_flags, int window_flags)
			{
				return imgui_BeginChild(id, width, height, child_flags, window_flags);
			}
			#region objectPoolingStuff
			//private constructor, needs to be created so that you are forced to use the pool.
			private ImGUIChild()
			{

			}
			public static ImGUIChild Aquire()
			{
				ImGUIChild obj;
				if (!StaticObjectPool.TryPop<ImGUIChild>(out obj))
				{
					obj = new ImGUIChild();
				}

				return obj;
			}
			public void Dispose()
			{
				imgui_EndChild();
				StaticObjectPool.Push(this);
			}
			~ImGUIChild()
			{
				//DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
				//if this is called, it will cause the domain to hang in the GC when shuttind down
				//This is only here to warn you

			}

			#endregion
		}
		public class PushStyle : IDisposable
		{

			public void PushStyleColor(int type, float r, float g, float b, float a)
			{
				imgui_PushStyleColor(type, r, g, b, a);
			}
			#region objectPoolingStuff
			//private constructor, needs to be created so that you are forced to use the pool.
			private PushStyle()
			{

			}
			public static PushStyle Aquire()
			{
				PushStyle obj;
				if (!StaticObjectPool.TryPop<PushStyle>(out obj))
				{
					obj = new PushStyle();
				}

				return obj;
			}
			public void Dispose()
			{
				imgui_PopStyleColor(1);
				StaticObjectPool.Push(this);
			}
			~PushStyle()
			{
				//DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
				//if this is called, it will cause the domain to hang in the GC when shuttind down
				//This is only here to warn you

			}

			#endregion
		}
		public class IMGUI_Fonts : IDisposable
		{
			bool fontChanged = false;
			public void PushFont(string font)
			{
				string fontToUse = font;
				if (FontList.ContainsKey(font))
				{
					fontToUse = FontList[font];
				}
				fontChanged = imgui_PushFont(fontToUse);
			}
			#region objectPoolingStuff
			//private constructor, needs to be created so that you are forced to use the pool.
			private IMGUI_Fonts()
			{

			}
			public static IMGUI_Fonts Aquire()
			{
				IMGUI_Fonts obj;
				if (!StaticObjectPool.TryPop<IMGUI_Fonts>(out obj))
				{
					obj = new IMGUI_Fonts();
				}

				return obj;
			}
			public void Dispose()
			{
				if(fontChanged)
				{
					imgui_PopFont();
				}
				StaticObjectPool.Push(this);
			}
			~IMGUI_Fonts()
			{
				//DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
				//if this is called, it will cause the domain to hang in the GC when shuttind down
				//This is only here to warn you

			}

			#endregion
		}

	}
}
