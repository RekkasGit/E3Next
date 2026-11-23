using CommunityToolkit.HighPerformance.Buffers;
using E3Core.Data;
using E3Core.Processors;
using E3Core.Utility;
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
		public static Dictionary<String, string> FontList = new Dictionary<string, string>() { {"robo","RobotoRegular" }//, {"robo-large", "RobotoRegular (Large)" }
		,{"lucon.ttf","lucon.ttf" }
		,{"EQ-Thin", "4" }
		//,{"EQ-1", "1" }
		//,{"EQ-2", "2" }
		//,{"EQ-3","3" }
		//,{"EQ-4","4" }
		//,{"EQ-5","5" }
		//,{"EQ-6","6" }
		//,{"EQ-7","7" }
		//,{"EQ-8","8" }
		//,{"EQ-9","9" }
		,{"EQ-Bold","10" }};
		public static class MaterialFont
		{
			public const string threed_rotation = "\ue84d";
			public const string ac_unit = "\ueb3b";
			public const string access_alarm = "\ue190";
			public const string access_alarms = "\ue191";
			public const string access_time = "\ue192";
			public const string accessibility = "\ue84e";
			public const string accessible = "\ue914";
			public const string account_balance = "\ue84f";
			public const string account_balance_wallet = "\ue850";
			public const string account_box = "\ue851";
			public const string account_circle = "\ue853";
			public const string adb = "\ue60e";
			public const string add = "\ue145";
			public const string add_a_photo = "\ue439";
			public const string add_alarm = "\ue193";
			public const string add_alert = "\ue003";
			public const string add_box = "\ue146";
			public const string add_circle = "\ue147";
			public const string add_circle_outline = "\ue148";
			public const string add_location = "\ue567";
			public const string add_shopping_cart = "\ue854";
			public const string add_to_photos = "\ue39d";
			public const string add_to_queue = "\ue05c";
			public const string adjust = "\ue39e";
			public const string airline_seat_flat = "\ue630";
			public const string airline_seat_flat_angled = "\ue631";
			public const string airline_seat_individual_suite = "\ue632";
			public const string airline_seat_legroom_extra = "\ue633";
			public const string airline_seat_legroom_normal = "\ue634";
			public const string airline_seat_legroom_reduced = "\ue635";
			public const string airline_seat_recline_extra = "\ue636";
			public const string airline_seat_recline_normal = "\ue637";
			public const string airplanemode_active = "\ue195";
			public const string airplanemode_inactive = "\ue194";
			public const string airplay = "\ue055";
			public const string airport_shuttle = "\ueb3c";
			public const string alarm = "\ue855";
			public const string alarm_add = "\ue856";
			public const string alarm_off = "\ue857";
			public const string alarm_on = "\ue858";
			public const string album = "\ue019";
			public const string all_inclusive = "\ueb3d";
			public const string all_out = "\ue90b";
			public const string android = "\ue859";
			public const string announcement = "\ue85a";
			public const string apps = "\ue5c3";
			public const string archive = "\ue149";
			public const string arrow_back = "\ue5c4";
			public const string arrow_downward = "\ue5db";
			public const string arrow_drop_down = "\ue5c5";
			public const string arrow_drop_down_circle = "\ue5c6";
			public const string arrow_drop_up = "\ue5c7";
			public const string arrow_forward = "\ue5c8";
			public const string arrow_upward = "\ue5d8";
			public const string art_track = "\ue060";
			public const string aspect_ratio = "\ue85b";
			public const string assessment = "\ue85c";
			public const string assignment = "\ue85d";
			public const string assignment_ind = "\ue85e";
			public const string assignment_late = "\ue85f";
			public const string assignment_return = "\ue860";
			public const string assignment_returned = "\ue861";
			public const string assignment_turned_in = "\ue862";
			public const string assistant = "\ue39f";
			public const string assistant_photo = "\ue3a0";
			public const string attach_file = "\ue226";
			public const string attach_money = "\ue227";
			public const string attachment = "\ue2bc";
			public const string audiotrack = "\ue3a1";
			public const string autorenew = "\ue863";
			public const string av_timer = "\ue01b";
			public const string backspace = "\ue14a";
			public const string backup = "\ue864";
			public const string battery_alert = "\ue19c";
			public const string battery_charging_full = "\ue1a3";
			public const string battery_full = "\ue1a4";
			public const string battery_std = "\ue1a5";
			public const string battery_unknown = "\ue1a6";
			public const string beach_access = "\ueb3e";
			public const string beenhere = "\ue52d";
			public const string block = "\ue14b";
			public const string bluetooth = "\ue1a7";
			public const string bluetooth_audio = "\ue60f";
			public const string bluetooth_connected = "\ue1a8";
			public const string bluetooth_disabled = "\ue1a9";
			public const string bluetooth_searching = "\ue1aa";
			public const string blur_circular = "\ue3a2";
			public const string blur_linear = "\ue3a3";
			public const string blur_off = "\ue3a4";
			public const string blur_on = "\ue3a5";
			public const string book = "\ue865";
			public const string bookmark = "\ue866";
			public const string bookmark_border = "\ue867";
			public const string border_all = "\ue228";
			public const string border_bottom = "\ue229";
			public const string border_clear = "\ue22a";
			public const string border_color = "\ue22b";
			public const string border_horizontal = "\ue22c";
			public const string border_inner = "\ue22d";
			public const string border_left = "\ue22e";
			public const string border_outer = "\ue22f";
			public const string border_right = "\ue230";
			public const string border_style = "\ue231";
			public const string border_top = "\ue232";
			public const string border_vertical = "\ue233";
			public const string branding_watermark = "\ue06b";
			public const string brightness_1 = "\ue3a6";
			public const string brightness_2 = "\ue3a7";
			public const string brightness_3 = "\ue3a8";
			public const string brightness_4 = "\ue3a9";
			public const string brightness_5 = "\ue3aa";
			public const string brightness_6 = "\ue3ab";
			public const string brightness_7 = "\ue3ac";
			public const string brightness_auto = "\ue1ab";
			public const string brightness_high = "\ue1ac";
			public const string brightness_low = "\ue1ad";
			public const string brightness_medium = "\ue1ae";
			public const string broken_image = "\ue3ad";
			public const string brush = "\ue3ae";
			public const string bubble_chart = "\ue6dd";
			public const string bug_report = "\ue868";
			public const string build = "\ue869";
			public const string burst_mode = "\ue43c";
			public const string business = "\ue0af";
			public const string business_center = "\ueb3f";
			public const string cached = "\ue86a";
			public const string cake = "\ue7e9";
			public const string call = "\ue0b0";
			public const string call_end = "\ue0b1";
			public const string call_made = "\ue0b2";
			public const string call_merge = "\ue0b3";
			public const string call_missed = "\ue0b4";
			public const string call_missed_outgoing = "\ue0e4";
			public const string call_received = "\ue0b5";
			public const string call_split = "\ue0b6";
			public const string call_to_action = "\ue06c";
			public const string camera = "\ue3af";
			public const string camera_alt = "\ue3b0";
			public const string camera_enhance = "\ue8fc";
			public const string camera_front = "\ue3b1";
			public const string camera_rear = "\ue3b2";
			public const string camera_roll = "\ue3b3";
			public const string cancel = "\ue5c9";
			public const string card_giftcard = "\ue8f6";
			public const string card_membership = "\ue8f7";
			public const string card_travel = "\ue8f8";
			public const string casino = "\ueb40";
			public const string cast = "\ue307";
			public const string cast_connected = "\ue308";
			public const string center_focus_strong = "\ue3b4";
			public const string center_focus_weak = "\ue3b5";
			public const string change_history = "\ue86b";
			public const string chat = "\ue0b7";
			public const string chat_bubble = "\ue0ca";
			public const string chat_bubble_outline = "\ue0cb";
			public const string check = "\ue5ca";
			public const string check_box = "\ue834";
			public const string check_box_outline_blank = "\ue835";
			public const string check_circle = "\ue86c";
			public const string chevron_left = "\ue5cb";
			public const string chevron_right = "\ue5cc";
			public const string child_care = "\ueb41";
			public const string child_friendly = "\ueb42";
			public const string chrome_reader_mode = "\ue86d";
			public const string classclass = "\ue86e";
			public const string clear = "\ue14c";
			public const string clear_all = "\ue0b8";
			public const string close = "\ue5cd";
			public const string closed_caption = "\ue01c";
			public const string cloud = "\ue2bd";
			public const string cloud_circle = "\ue2be";
			public const string cloud_done = "\ue2bf";
			public const string cloud_download = "\ue2c0";
			public const string cloud_off = "\ue2c1";
			public const string cloud_queue = "\ue2c2";
			public const string cloud_upload = "\ue2c3";
			public const string code = "\ue86f";
			public const string collections = "\ue3b6";
			public const string collections_bookmark = "\ue431";
			public const string color_lens = "\ue3b7";
			public const string colorize = "\ue3b8";
			public const string comment = "\ue0b9";
			public const string compare = "\ue3b9";
			public const string compare_arrows = "\ue915";
			public const string computer = "\ue30a";
			public const string confirmation_number = "\ue638";
			public const string contact_mail = "\ue0d0";
			public const string contact_phone = "\ue0cf";
			public const string contacts = "\ue0ba";
			public const string content_copy = "\ue14d";
			public const string content_cut = "\ue14e";
			public const string content_paste = "\ue14f";
			public const string control_point = "\ue3ba";
			public const string control_point_duplicate = "\ue3bb";
			public const string copyright = "\ue90c";
			public const string create = "\ue150";
			public const string create_new_folder = "\ue2cc";
			public const string credit_card = "\ue870";
			public const string crop = "\ue3be";
			public const string crop_16_9 = "\ue3bc";
			public const string crop_3_2 = "\ue3bd";
			public const string crop_5_4 = "\ue3bf";
			public const string crop_7_5 = "\ue3c0";
			public const string crop_din = "\ue3c1";
			public const string crop_free = "\ue3c2";
			public const string crop_landscape = "\ue3c3";
			public const string crop_original = "\ue3c4";
			public const string crop_portrait = "\ue3c5";
			public const string crop_rotate = "\ue437";
			public const string crop_square = "\ue3c6";
			public const string dashboard = "\ue871";
			public const string data_usage = "\ue1af";
			public const string date_range = "\ue916";
			public const string dehaze = "\ue3c7";
			public const string delete = "\ue872";
			public const string delete_forever = "\ue92b";
			public const string delete_sweep = "\ue16c";
			public const string description = "\ue873";
			public const string desktop_mac = "\ue30b";
			public const string desktop_windows = "\ue30c";
			public const string details = "\ue3c8";
			public const string developer_board = "\ue30d";
			public const string developer_mode = "\ue1b0";
			public const string device_hub = "\ue335";
			public const string devices = "\ue1b1";
			public const string devices_other = "\ue337";
			public const string dialer_sip = "\ue0bb";
			public const string dialpad = "\ue0bc";
			public const string directions = "\ue52e";
			public const string directions_bike = "\ue52f";
			public const string directions_boat = "\ue532";
			public const string directions_bus = "\ue530";
			public const string directions_car = "\ue531";
			public const string directions_railway = "\ue534";
			public const string directions_run = "\ue566";
			public const string directions_subway = "\ue533";
			public const string directions_transit = "\ue535";
			public const string directions_walk = "\ue536";
			public const string disc_full = "\ue610";
			public const string dns = "\ue875";
			public const string do_not_disturb = "\ue612";
			public const string do_not_disturb_alt = "\ue611";
			public const string do_not_disturb_off = "\ue643";
			public const string do_not_disturb_on = "\ue644";
			public const string dock = "\ue30e";
			public const string domain = "\ue7ee";
			public const string done = "\ue876";
			public const string done_all = "\ue877";
			public const string donut_large = "\ue917";
			public const string donut_small = "\ue918";
			public const string drafts = "\ue151";
			public const string drag_handle = "\ue25d";
			public const string drive_eta = "\ue613";
			public const string dvr = "\ue1b2";
			public const string edit = "\ue3c9";
			public const string edit_location = "\ue568";
			public const string eject = "\ue8fb";
			public const string email = "\ue0be";
			public const string enhanced_encryption = "\ue63f";
			public const string equalizer = "\ue01d";
			public const string error = "\ue000";
			public const string error_outline = "\ue001";
			public const string euro_symbol = "\ue926";
			public const string ev_station = "\ue56d";
			public const string eventevent = "\ue878";
			public const string event_available = "\ue614";
			public const string event_busy = "\ue615";
			public const string event_note = "\ue616";
			public const string event_seat = "\ue903";
			public const string exit_to_app = "\ue879";
			public const string expand_less = "\ue5ce";
			public const string expand_more = "\ue5cf";
			public const string explicitexplicit = "\ue01e";
			public const string explore = "\ue87a";
			public const string exposure = "\ue3ca";
			public const string exposure_neg_1 = "\ue3cb";
			public const string exposure_neg_2 = "\ue3cc";
			public const string exposure_plus_1 = "\ue3cd";
			public const string exposure_plus_2 = "\ue3ce";
			public const string exposure_zero = "\ue3cf";
			public const string extension = "\ue87b";
			public const string face = "\ue87c";
			public const string fast_forward = "\ue01f";
			public const string fast_rewind = "\ue020";
			public const string favorite = "\ue87d";
			public const string favorite_border = "\ue87e";
			public const string featured_play_list = "\ue06d";
			public const string featured_video = "\ue06e";
			public const string feedback = "\ue87f";
			public const string fiber_dvr = "\ue05d";
			public const string fiber_manual_record = "\ue061";
			public const string fiber_new = "\ue05e";
			public const string fiber_pin = "\ue06a";
			public const string fiber_smart_record = "\ue062";
			public const string file_download = "\ue2c4";
			public const string file_upload = "\ue2c6";
			public const string filter = "\ue3d3";
			public const string filter_1 = "\ue3d0";
			public const string filter_2 = "\ue3d1";
			public const string filter_3 = "\ue3d2";
			public const string filter_4 = "\ue3d4";
			public const string filter_5 = "\ue3d5";
			public const string filter_6 = "\ue3d6";
			public const string filter_7 = "\ue3d7";
			public const string filter_8 = "\ue3d8";
			public const string filter_9 = "\ue3d9";
			public const string filter_9_plus = "\ue3da";
			public const string filter_b_and_w = "\ue3db";
			public const string filter_center_focus = "\ue3dc";
			public const string filter_drama = "\ue3dd";
			public const string filter_frames = "\ue3de";
			public const string filter_hdr = "\ue3df";
			public const string filter_list = "\ue152";
			public const string filter_none = "\ue3e0";
			public const string filter_tilt_shift = "\ue3e2";
			public const string filter_vintage = "\ue3e3";
			public const string find_in_page = "\ue880";
			public const string find_replace = "\ue881";
			public const string fingerprint = "\ue90d";
			public const string first_page = "\ue5dc";
			public const string fitness_center = "\ueb43";
			public const string flag = "\ue153";
			public const string flare = "\ue3e4";
			public const string flash_auto = "\ue3e5";
			public const string flash_off = "\ue3e6";
			public const string flash_on = "\ue3e7";
			public const string flight = "\ue539";
			public const string flight_land = "\ue904";
			public const string flight_takeoff = "\ue905";
			public const string flip = "\ue3e8";
			public const string flip_to_back = "\ue882";
			public const string flip_to_front = "\ue883";
			public const string folder = "\ue2c7";
			public const string folder_open = "\ue2c8";
			public const string folder_shared = "\ue2c9";
			public const string folder_special = "\ue617";
			public const string font_download = "\ue167";
			public const string format_align_center = "\ue234";
			public const string format_align_justify = "\ue235";
			public const string format_align_left = "\ue236";
			public const string format_align_right = "\ue237";
			public const string format_bold = "\ue238";
			public const string format_clear = "\ue239";
			public const string format_color_fill = "\ue23a";
			public const string format_color_reset = "\ue23b";
			public const string format_color_text = "\ue23c";
			public const string format_indent_decrease = "\ue23d";
			public const string format_indent_increase = "\ue23e";
			public const string format_italic = "\ue23f";
			public const string format_line_spacing = "\ue240";
			public const string format_list_bulleted = "\ue241";
			public const string format_list_numbered = "\ue242";
			public const string format_paint = "\ue243";
			public const string format_quote = "\ue244";
			public const string format_shapes = "\ue25e";
			public const string format_size = "\ue245";
			public const string format_strikethrough = "\ue246";
			public const string format_textdirection_l_to_r = "\ue247";
			public const string format_textdirection_r_to_l = "\ue248";
			public const string format_underlined = "\ue249";
			public const string forum = "\ue0bf";
			public const string forward = "\ue154";
			public const string forward_10 = "\ue056";
			public const string forward_30 = "\ue057";
			public const string forward_5 = "\ue058";
			public const string free_breakfast = "\ueb44";
			public const string fullscreen = "\ue5d0";
			public const string fullscreen_exit = "\ue5d1";
			public const string functions = "\ue24a";
			public const string g_translate = "\ue927";
			public const string gamepad = "\ue30f";
			public const string games = "\ue021";
			public const string gavel = "\ue90e";
			public const string gesture = "\ue155";
			public const string get_app = "\ue884";
			public const string gif = "\ue908";
			public const string golf_course = "\ueb45";
			public const string gps_fixed = "\ue1b3";
			public const string gps_not_fixed = "\ue1b4";
			public const string gps_off = "\ue1b5";
			public const string grade = "\ue885";
			public const string gradient = "\ue3e9";
			public const string grain = "\ue3ea";
			public const string graphic_eq = "\ue1b8";
			public const string grid_off = "\ue3eb";
			public const string grid_on = "\ue3ec";
			public const string group = "\ue7ef";
			public const string group_add = "\ue7f0";
			public const string group_work = "\ue886";
			public const string hd = "\ue052";
			public const string hdr_off = "\ue3ed";
			public const string hdr_on = "\ue3ee";
			public const string hdr_strong = "\ue3f1";
			public const string hdr_weak = "\ue3f2";
			public const string headset = "\ue310";
			public const string headset_mic = "\ue311";
			public const string healing = "\ue3f3";
			public const string hearing = "\ue023";
			public const string help = "\ue887";
			public const string help_outline = "\ue8fd";
			public const string high_quality = "\ue024";
			public const string highlight = "\ue25f";
			public const string highlight_off = "\ue888";
			public const string history = "\ue889";
			public const string home = "\ue88a";
			public const string hot_tub = "\ueb46";
			public const string hotel = "\ue53a";
			public const string hourglass_empty = "\ue88b";
			public const string hourglass_full = "\ue88c";
			public const string http = "\ue902";
			public const string https = "\ue88d";
			public const string image = "\ue3f4";
			public const string image_aspect_ratio = "\ue3f5";
			public const string import_contacts = "\ue0e0";
			public const string import_export = "\ue0c3";
			public const string important_devices = "\ue912";
			public const string inbox = "\ue156";
			public const string indeterminate_check_box = "\ue909";
			public const string info = "\ue88e";
			public const string info_outline = "\ue88f";
			public const string input = "\ue890";
			public const string insert_chart = "\ue24b";
			public const string insert_comment = "\ue24c";
			public const string insert_drive_file = "\ue24d";
			public const string insert_emoticon = "\ue24e";
			public const string insert_invitation = "\ue24f";
			public const string insert_link = "\ue250";
			public const string insert_photo = "\ue251";
			public const string invert_colors = "\ue891";
			public const string invert_colors_off = "\ue0c4";
			public const string iso = "\ue3f6";
			public const string keyboard = "\ue312";
			public const string keyboard_arrow_down = "\ue313";
			public const string keyboard_arrow_left = "\ue314";
			public const string keyboard_arrow_right = "\ue315";
			public const string keyboard_arrow_up = "\ue316";
			public const string keyboard_backspace = "\ue317";
			public const string keyboard_capslock = "\ue318";
			public const string keyboard_hide = "\ue31a";
			public const string keyboard_return = "\ue31b";
			public const string keyboard_tab = "\ue31c";
			public const string keyboard_voice = "\ue31d";
			public const string kitchen = "\ueb47";
			public const string label = "\ue892";
			public const string label_outline = "\ue893";
			public const string landscape = "\ue3f7";
			public const string language = "\ue894";
			public const string laptop = "\ue31e";
			public const string laptop_chromebook = "\ue31f";
			public const string laptop_mac = "\ue320";
			public const string laptop_windows = "\ue321";
			public const string last_page = "\ue5dd";
			public const string launch = "\ue895";
			public const string layers = "\ue53b";
			public const string layers_clear = "\ue53c";
			public const string leak_add = "\ue3f8";
			public const string leak_remove = "\ue3f9";
			public const string lens = "\ue3fa";
			public const string library_add = "\ue02e";
			public const string library_books = "\ue02f";
			public const string library_music = "\ue030";
			public const string lightbulb_outline = "\ue90f";
			public const string line_style = "\ue919";
			public const string line_weight = "\ue91a";
			public const string linear_scale = "\ue260";
			public const string link = "\ue157";
			public const string linked_camera = "\ue438";
			public const string list = "\ue896";
			public const string live_help = "\ue0c6";
			public const string live_tv = "\ue639";
			public const string local_activity = "\ue53f";
			public const string local_airport = "\ue53d";
			public const string local_atm = "\ue53e";
			public const string local_bar = "\ue540";
			public const string local_cafe = "\ue541";
			public const string local_car_wash = "\ue542";
			public const string local_convenience_store = "\ue543";
			public const string local_dining = "\ue556";
			public const string local_drink = "\ue544";
			public const string local_florist = "\ue545";
			public const string local_gas_station = "\ue546";
			public const string local_grocery_store = "\ue547";
			public const string local_hospital = "\ue548";
			public const string local_hotel = "\ue549";
			public const string local_laundry_service = "\ue54a";
			public const string local_library = "\ue54b";
			public const string local_mall = "\ue54c";
			public const string local_movies = "\ue54d";
			public const string local_offer = "\ue54e";
			public const string local_parking = "\ue54f";
			public const string local_pharmacy = "\ue550";
			public const string local_phone = "\ue551";
			public const string local_pizza = "\ue552";
			public const string local_play = "\ue553";
			public const string local_post_office = "\ue554";
			public const string local_printshop = "\ue555";
			public const string local_see = "\ue557";
			public const string local_shipping = "\ue558";
			public const string local_taxi = "\ue559";
			public const string location_city = "\ue7f1";
			public const string location_disabled = "\ue1b6";
			public const string location_off = "\ue0c7";
			public const string location_on = "\ue0c8";
			public const string location_searching = "\ue1b7";
			public const string locklock = "\ue897";
			public const string lock_open = "\ue898";
			public const string lock_outline = "\ue899";
			public const string looks = "\ue3fc";
			public const string looks_3 = "\ue3fb";
			public const string looks_4 = "\ue3fd";
			public const string looks_5 = "\ue3fe";
			public const string looks_6 = "\ue3ff";
			public const string looks_one = "\ue400";
			public const string looks_two = "\ue401";
			public const string loop = "\ue028";
			public const string loupe = "\ue402";
			public const string low_priority = "\ue16d";
			public const string loyalty = "\ue89a";
			public const string mail = "\ue158";
			public const string mail_outline = "\ue0e1";
			public const string map = "\ue55b";
			public const string markunread = "\ue159";
			public const string markunread_mailbox = "\ue89b";
			public const string memory = "\ue322";
			public const string menu = "\ue5d2";
			public const string merge_type = "\ue252";
			public const string message = "\ue0c9";
			public const string mic = "\ue029";
			public const string mic_none = "\ue02a";
			public const string mic_off = "\ue02b";
			public const string mms = "\ue618";
			public const string mode_comment = "\ue253";
			public const string mode_edit = "\ue254";
			public const string monetization_on = "\ue263";
			public const string money_off = "\ue25c";
			public const string monochrome_photos = "\ue403";
			public const string mood = "\ue7f2";
			public const string mood_bad = "\ue7f3";
			public const string more = "\ue619";
			public const string more_horiz = "\ue5d3";
			public const string more_vert = "\ue5d4";
			public const string motorcycle = "\ue91b";
			public const string mouse = "\ue323";
			public const string move_to_inbox = "\ue168";
			public const string movie = "\ue02c";
			public const string movie_creation = "\ue404";
			public const string movie_filter = "\ue43a";
			public const string multiline_chart = "\ue6df";
			public const string music_note = "\ue405";
			public const string music_video = "\ue063";
			public const string my_location = "\ue55c";
			public const string nature = "\ue406";
			public const string nature_people = "\ue407";
			public const string navigate_before = "\ue408";
			public const string navigate_next = "\ue409";
			public const string navigation = "\ue55d";
			public const string near_me = "\ue569";
			public const string network_cell = "\ue1b9";
			public const string network_check = "\ue640";
			public const string network_locked = "\ue61a";
			public const string network_wifi = "\ue1ba";
			public const string new_releases = "\ue031";
			public const string next_week = "\ue16a";
			public const string nfc = "\ue1bb";
			public const string no_encryption = "\ue641";
			public const string no_sim = "\ue0cc";
			public const string not_interested = "\ue033";
			public const string note = "\ue06f";
			public const string note_add = "\ue89c";
			public const string notifications = "\ue7f4";
			public const string notifications_active = "\ue7f7";
			public const string notifications_none = "\ue7f5";
			public const string notifications_off = "\ue7f6";
			public const string notifications_paused = "\ue7f8";
			public const string offline_pin = "\ue90a";
			public const string ondemand_video = "\ue63a";
			public const string opacity = "\ue91c";
			public const string open_in_browser = "\ue89d";
			public const string open_in_new = "\ue89e";
			public const string open_with = "\ue89f";
			public const string pages = "\ue7f9";
			public const string pageview = "\ue8a0";
			public const string palette = "\ue40a";
			public const string pan_tool = "\ue925";
			public const string panorama = "\ue40b";
			public const string panorama_fish_eye = "\ue40c";
			public const string panorama_horizontal = "\ue40d";
			public const string panorama_vertical = "\ue40e";
			public const string panorama_wide_angle = "\ue40f";
			public const string party_mode = "\ue7fa";
			public const string pause = "\ue034";
			public const string pause_circle_filled = "\ue035";
			public const string pause_circle_outline = "\ue036";
			public const string payment = "\ue8a1";
			public const string people = "\ue7fb";
			public const string people_outline = "\ue7fc";
			public const string perm_camera_mic = "\ue8a2";
			public const string perm_contact_calendar = "\ue8a3";
			public const string perm_data_setting = "\ue8a4";
			public const string perm_device_information = "\ue8a5";
			public const string perm_identity = "\ue8a6";
			public const string perm_media = "\ue8a7";
			public const string perm_phone_msg = "\ue8a8";
			public const string perm_scan_wifi = "\ue8a9";
			public const string person = "\ue7fd";
			public const string person_add = "\ue7fe";
			public const string person_outline = "\ue7ff";
			public const string person_pin = "\ue55a";
			public const string person_pin_circle = "\ue56a";
			public const string personal_video = "\ue63b";
			public const string pets = "\ue91d";
			public const string phone = "\ue0cd";
			public const string phone_android = "\ue324";
			public const string phone_bluetooth_speaker = "\ue61b";
			public const string phone_forwarded = "\ue61c";
			public const string phone_in_talk = "\ue61d";
			public const string phone_iphone = "\ue325";
			public const string phone_locked = "\ue61e";
			public const string phone_missed = "\ue61f";
			public const string phone_paused = "\ue620";
			public const string phonelink = "\ue326";
			public const string phonelink_erase = "\ue0db";
			public const string phonelink_lock = "\ue0dc";
			public const string phonelink_off = "\ue327";
			public const string phonelink_ring = "\ue0dd";
			public const string phonelink_setup = "\ue0de";
			public const string photo = "\ue410";
			public const string photo_album = "\ue411";
			public const string photo_camera = "\ue412";
			public const string photo_filter = "\ue43b";
			public const string photo_library = "\ue413";
			public const string photo_size_select_actual = "\ue432";
			public const string photo_size_select_large = "\ue433";
			public const string photo_size_select_small = "\ue434";
			public const string picture_as_pdf = "\ue415";
			public const string picture_in_picture = "\ue8aa";
			public const string picture_in_picture_alt = "\ue911";
			public const string pie_chart = "\ue6c4";
			public const string pie_chart_outlined = "\ue6c5";
			public const string pin_drop = "\ue55e";
			public const string place = "\ue55f";
			public const string play_arrow = "\ue037";
			public const string play_circle_filled = "\ue038";
			public const string play_circle_outline = "\ue039";
			public const string play_for_work = "\ue906";
			public const string playlist_add = "\ue03b";
			public const string playlist_add_check = "\ue065";
			public const string playlist_play = "\ue05f";
			public const string plus_one = "\ue800";
			public const string poll = "\ue801";
			public const string polymer = "\ue8ab";
			public const string pool = "\ueb48";
			public const string portable_wifi_off = "\ue0ce";
			public const string portrait = "\ue416";
			public const string power = "\ue63c";
			public const string power_input = "\ue336";
			public const string power_settings_new = "\ue8ac";
			public const string pregnant_woman = "\ue91e";
			public const string present_to_all = "\ue0df";
			public const string print = "\ue8ad";
			public const string priority_high = "\ue645";
			public const string publicpublic = "\ue80b";
			public const string publish = "\ue255";
			public const string query_builder = "\ue8ae";
			public const string question_answer = "\ue8af";
			public const string queue = "\ue03c";
			public const string queue_music = "\ue03d";
			public const string queue_play_next = "\ue066";
			public const string radio = "\ue03e";
			public const string radio_button_checked = "\ue837";
			public const string radio_button_unchecked = "\ue836";
			public const string rate_review = "\ue560";
			public const string receipt = "\ue8b0";
			public const string recent_actors = "\ue03f";
			public const string record_voice_over = "\ue91f";
			public const string redeem = "\ue8b1";
			public const string redo = "\ue15a";
			public const string refresh = "\ue5d5";
			public const string remove = "\ue15b";
			public const string remove_circle = "\ue15c";
			public const string remove_circle_outline = "\ue15d";
			public const string remove_from_queue = "\ue067";
			public const string remove_red_eye = "\ue417";
			public const string remove_shopping_cart = "\ue928";
			public const string reorder = "\ue8fe";
			public const string repeat = "\ue040";
			public const string repeat_one = "\ue041";
			public const string replay = "\ue042";
			public const string replay_10 = "\ue059";
			public const string replay_30 = "\ue05a";
			public const string replay_5 = "\ue05b";
			public const string reply = "\ue15e";
			public const string reply_all = "\ue15f";
			public const string report = "\ue160";
			public const string report_problem = "\ue8b2";
			public const string restaurant = "\ue56c";
			public const string restaurant_menu = "\ue561";
			public const string restore = "\ue8b3";
			public const string restore_page = "\ue929";
			public const string ring_volume = "\ue0d1";
			public const string room = "\ue8b4";
			public const string room_service = "\ueb49";
			public const string rotate_90_degrees_ccw = "\ue418";
			public const string rotate_left = "\ue419";
			public const string rotate_right = "\ue41a";
			public const string rounded_corner = "\ue920";
			public const string router = "\ue328";
			public const string rowing = "\ue921";
			public const string rss_feed = "\ue0e5";
			public const string rv_hookup = "\ue642";
			public const string satellite = "\ue562";
			public const string save = "\ue161";
			public const string scanner = "\ue329";
			public const string schedule = "\ue8b5";
			public const string school = "\ue80c";
			public const string screen_lock_landscape = "\ue1be";
			public const string screen_lock_portrait = "\ue1bf";
			public const string screen_lock_rotation = "\ue1c0";
			public const string screen_rotation = "\ue1c1";
			public const string screen_share = "\ue0e2";
			public const string sd_card = "\ue623";
			public const string sd_storage = "\ue1c2";
			public const string search = "\ue8b6";
			public const string security = "\ue32a";
			public const string select_all = "\ue162";
			public const string send = "\ue163";
			public const string sentiment_dissatisfied = "\ue811";
			public const string sentiment_neutral = "\ue812";
			public const string sentiment_satisfied = "\ue813";
			public const string sentiment_very_dissatisfied = "\ue814";
			public const string sentiment_very_satisfied = "\ue815";
			public const string settings = "\ue8b8";
			public const string settings_applications = "\ue8b9";
			public const string settings_backup_restore = "\ue8ba";
			public const string settings_bluetooth = "\ue8bb";
			public const string settings_brightness = "\ue8bd";
			public const string settings_cell = "\ue8bc";
			public const string settings_ethernet = "\ue8be";
			public const string settings_input_antenna = "\ue8bf";
			public const string settings_input_component = "\ue8c0";
			public const string settings_input_composite = "\ue8c1";
			public const string settings_input_hdmi = "\ue8c2";
			public const string settings_input_svideo = "\ue8c3";
			public const string settings_overscan = "\ue8c4";
			public const string settings_phone = "\ue8c5";
			public const string settings_power = "\ue8c6";
			public const string settings_remote = "\ue8c7";
			public const string settings_system_daydream = "\ue1c3";
			public const string settings_voice = "\ue8c8";
			public const string share = "\ue80d";
			public const string shop = "\ue8c9";
			public const string shop_two = "\ue8ca";
			public const string shopping_basket = "\ue8cb";
			public const string shopping_cart = "\ue8cc";
			public const string short_text = "\ue261";
			public const string show_chart = "\ue6e1";
			public const string shuffle = "\ue043";
			public const string signal_cellular_4_bar = "\ue1c8";
			public const string signal_cellular_connected_no_internet_4_bar = "\ue1cd";
			public const string signal_cellular_no_sim = "\ue1ce";
			public const string signal_cellular_null = "\ue1cf";
			public const string signal_cellular_off = "\ue1d0";
			public const string signal_wifi_4_bar = "\ue1d8";
			public const string signal_wifi_4_bar_lock = "\ue1d9";
			public const string signal_wifi_off = "\ue1da";
			public const string sim_card = "\ue32b";
			public const string sim_card_alert = "\ue624";
			public const string skip_next = "\ue044";
			public const string skip_previous = "\ue045";
			public const string slideshow = "\ue41b";
			public const string slow_motion_video = "\ue068";
			public const string smartphone = "\ue32c";
			public const string smoke_free = "\ueb4a";
			public const string smoking_rooms = "\ueb4b";
			public const string sms = "\ue625";
			public const string sms_failed = "\ue626";
			public const string snooze = "\ue046";
			public const string sort = "\ue164";
			public const string sort_by_alpha = "\ue053";
			public const string spa = "\ueb4c";
			public const string space_bar = "\ue256";
			public const string speaker = "\ue32d";
			public const string speaker_group = "\ue32e";
			public const string speaker_notes = "\ue8cd";
			public const string speaker_notes_off = "\ue92a";
			public const string speaker_phone = "\ue0d2";
			public const string spellcheck = "\ue8ce";
			public const string star = "\ue838";
			public const string star_border = "\ue83a";
			public const string star_half = "\ue839";
			public const string stars = "\ue8d0";
			public const string stay_current_landscape = "\ue0d3";
			public const string stay_current_portrait = "\ue0d4";
			public const string stay_primary_landscape = "\ue0d5";
			public const string stay_primary_portrait = "\ue0d6";
			public const string stop = "\ue047";
			public const string stop_screen_share = "\ue0e3";
			public const string storage = "\ue1db";
			public const string store = "\ue8d1";
			public const string store_mall_directory = "\ue563";
			public const string straighten = "\ue41c";
			public const string streetview = "\ue56e";
			public const string strikethrough_s = "\ue257";
			public const string style = "\ue41d";
			public const string subdirectory_arrow_left = "\ue5d9";
			public const string subdirectory_arrow_right = "\ue5da";
			public const string subject = "\ue8d2";
			public const string subscriptions = "\ue064";
			public const string subtitles = "\ue048";
			public const string subway = "\ue56f";
			public const string supervisor_account = "\ue8d3";
			public const string surround_sound = "\ue049";
			public const string swap_calls = "\ue0d7";
			public const string swap_horiz = "\ue8d4";
			public const string swap_vert = "\ue8d5";
			public const string swap_vertical_circle = "\ue8d6";
			public const string switch_camera = "\ue41e";
			public const string switch_video = "\ue41f";
			public const string sync = "\ue627";
			public const string sync_disabled = "\ue628";
			public const string sync_problem = "\ue629";
			public const string system_update = "\ue62a";
			public const string system_update_alt = "\ue8d7";
			public const string tab = "\ue8d8";
			public const string tab_unselected = "\ue8d9";
			public const string tablet = "\ue32f";
			public const string tablet_android = "\ue330";
			public const string tablet_mac = "\ue331";
			public const string tag_faces = "\ue420";
			public const string tap_and_play = "\ue62b";
			public const string terrain = "\ue564";
			public const string text_fields = "\ue262";
			public const string text_format = "\ue165";
			public const string textsms = "\ue0d8";
			public const string texture = "\ue421";
			public const string theaters = "\ue8da";
			public const string thumb_down = "\ue8db";
			public const string thumb_up = "\ue8dc";
			public const string thumbs_up_down = "\ue8dd";
			public const string time_to_leave = "\ue62c";
			public const string timelapse = "\ue422";
			public const string timeline = "\ue922";
			public const string timer = "\ue425";
			public const string timer_10 = "\ue423";
			public const string timer_3 = "\ue424";
			public const string timer_off = "\ue426";
			public const string title = "\ue264";
			public const string toc = "\ue8de";
			public const string today = "\ue8df";
			public const string toll = "\ue8e0";
			public const string tonality = "\ue427";
			public const string touch_app = "\ue913";
			public const string toys = "\ue332";
			public const string track_changes = "\ue8e1";
			public const string traffic = "\ue565";
			public const string train = "\ue570";
			public const string tram = "\ue571";
			public const string transfer_within_a_station = "\ue572";
			public const string transform = "\ue428";
			public const string translate = "\ue8e2";
			public const string trending_down = "\ue8e3";
			public const string trending_flat = "\ue8e4";
			public const string trending_up = "\ue8e5";
			public const string tune = "\ue429";
			public const string turned_in = "\ue8e6";
			public const string turned_in_not = "\ue8e7";
			public const string tv = "\ue333";
			public const string unarchive = "\ue169";
			public const string undo = "\ue166";
			public const string unfold_less = "\ue5d6";
			public const string unfold_more = "\ue5d7";
			public const string update = "\ue923";
			public const string usb = "\ue1e0";
			public const string verified_user = "\ue8e8";
			public const string vertical_align_bottom = "\ue258";
			public const string vertical_align_center = "\ue259";
			public const string vertical_align_top = "\ue25a";
			public const string vibration = "\ue62d";
			public const string video_call = "\ue070";
			public const string video_label = "\ue071";
			public const string video_library = "\ue04a";
			public const string videocam = "\ue04b";
			public const string videocam_off = "\ue04c";
			public const string videogame_asset = "\ue338";
			public const string view_agenda = "\ue8e9";
			public const string view_array = "\ue8ea";
			public const string view_carousel = "\ue8eb";
			public const string view_column = "\ue8ec";
			public const string view_comfy = "\ue42a";
			public const string view_compact = "\ue42b";
			public const string view_day = "\ue8ed";
			public const string view_headline = "\ue8ee";
			public const string view_list = "\ue8ef";
			public const string view_module = "\ue8f0";
			public const string view_quilt = "\ue8f1";
			public const string view_stream = "\ue8f2";
			public const string view_week = "\ue8f3";
			public const string vignette = "\ue435";
			public const string visibility = "\ue8f4";
			public const string visibility_off = "\ue8f5";
			public const string voice_chat = "\ue62e";
			public const string voicemail = "\ue0d9";
			public const string volume_down = "\ue04d";
			public const string volume_mute = "\ue04e";
			public const string volume_off = "\ue04f";
			public const string volume_up = "\ue050";
			public const string vpn_key = "\ue0da";
			public const string vpn_lock = "\ue62f";
			public const string wallpaper = "\ue1bc";
			public const string warning = "\ue002";
			public const string watch = "\ue334";
			public const string watch_later = "\ue924";
			public const string wb_auto = "\ue42c";
			public const string wb_cloudy = "\ue42d";
			public const string wb_incandescent = "\ue42e";
			public const string wb_iridescent = "\ue436";
			public const string wb_sunny = "\ue430";
			public const string wc = "\ue63d";
			public const string web = "\ue051";
			public const string web_asset = "\ue069";
			public const string weekend = "\ue16b";
			public const string whatshot = "\ue80e";
			public const string widgets = "\ue1bd";
			public const string wifi = "\ue63e";
			public const string wifi_lock = "\ue1e1";
			public const string wifi_tethering = "\ue1e2";
			public const string work = "\ue8f9";
			public const string wrap_text = "\ue25b";
			public const string youtube_searched_for = "\ue8fa";
			public const string zoom_in = "\ue8ff";
			public const string zoom_out = "\ue900";
			public const string zoom_out_map = "\ue56b";


		}
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

		// Dear ImGui's enums, numbered as the Dear ImGui the plugin draws with. Each is a static class of ints that its
		// Init sets for a version (ImGuiVersion.Version) and branch (ImGuiVersion.Docking):
		//   1.93 and later  Dear ImGui 1.93's numbers (Hailstone's HSMono);
		//   older           Dear ImGui 1.92.5's (MQ's MQ2Mono, docking branch). 1.92.6 to 1.92.9 differ from both in
		//                   places: popup flags (1.92.6), ImDrawFlags_Closed (1.92.8), new colours and style variables.
		// A name the Dear ImGui lacks (a docking one outside the docking branch, a 1.93 one in 1.92.5 ...) is -1 for an
		// index such as a colour, which HSMono ignores, or 0 for a flag, so that or-ing it in changes nothing.
		// Renamed names work with both: ImDrawFlags.RoundAll and RoundCornersAll are the same flag.
		// The docking names in 1.93 sit where 1.92.5's docking branch has them; not yet checked against 1.93's.
		// They are ints, not enums, so they can't be constants: a default argument or a case label takes a literal.
		public static class ImGuiVersion
		{
			public static int Version = 19300;   // IMGUI_VERSION_NUM: 19300 is 1.93.0, 19250 is 1.92.5
			public static bool Docking = false;  // the docking branch (IMGUI_HAS_DOCK)

			// Asks the plugin which Dear ImGui it draws with, then sets every enum below for it. Call it once from
			// OnInit, before any window is drawn.
			public static void Init()
			{
				try
				{
					QueryPlugin();
				}
				catch (Exception)
				{
					// No imgui_GetVersionNum: MQ2Mono, which draws with MQ's Dear ImGui 1.92.5, docking branch.
					Version = 19250;
					Docking = true;
				}
				InitAll(Version, Docking);
			}

			// Its own method: a missing internal call throws when the method calling it runs, which Init catches.
			[MethodImpl(MethodImplOptions.NoInlining)]
			static void QueryPlugin()
			{
				Version = imgui_GetVersionNum();
				Docking = imgui_HasDocking();
			}

			public static void InitAll(int version, bool docking)
			{
				Version = version;
				Docking = docking;
				ImGuiColorEditFlags.Init(version, docking);
				ImGuiPopupFlags.Init(version, docking);
				ImDrawFlags.Init(version, docking);
				ImGuiWindowFlags.Init(version, docking);
				ImGuiMouseButton.Init(version, docking);
				ImGuiSelectableFlags.Init(version, docking);
				ImGuiStyleVar.Init(version, docking);
				ImGuiChildFlags.Init(version, docking);
				ImGuiCol.Init(version, docking);
				ImGuiTableBgTarget.Init(version, docking);
				ImGuiCond.Init(version, docking);
				ImGuiTreeNodeFlags.Init(version, docking);
				ImGuiTableFlags.Init(version, docking);
				ImGuiTableColumnFlags.Init(version, docking);
			}
		}

		public static class ImGuiColorEditFlags
		{
			public static int None;
			public static int NoAlpha;   // ColorEdit, ColorPicker, ColorButton: ignore Alpha component (will only read 3 components from the input ...
			public static int NoPicker;   // ColorEdit: disable picker when clicking on color square.
			public static int NoOptions;   // ColorEdit: disable toggling options menu when right-clicking on inputs/small preview.
			public static int NoSmallPreview;   // ColorEdit, ColorPicker: disable color square preview next to the inputs. (e.g. to show only the inputs)
			public static int NoInputs;   // ColorEdit, ColorPicker: disable inputs sliders/text widgets (e.g. to show only the small preview color ...
			public static int NoTooltip;   // ColorEdit, ColorPicker, ColorButton: disable tooltip when hovering the preview.
			public static int NoLabel;   // ColorEdit, ColorPicker: disable display of inline text label (the label is still forwarded to the tooltip and ...
			public static int NoSidePreview;   // ColorPicker: disable bigger color preview on right side of the picker, use small color square preview instead.
			public static int NoDragDrop;   // ColorEdit: disable drag and drop target/source. ColorButton: disable drag and drop source.
			public static int NoBorder;   // ColorButton: disable border (which is enforced by default)
			public static int NoColorMarkers;   // [1.93 only] ColorEdit: disable rendering R/G/B/A color marker. May also be disabled globally by setting ...
			// Alpha preview
			public static int AlphaOpaque;   // ColorEdit, ColorPicker, ColorButton: disable alpha in the preview,. Contrary to _NoAlpha it may still be ...
			public static int AlphaNoBg;   // ColorEdit, ColorPicker, ColorButton: disable rendering a checkerboard background behind transparent color.
			public static int AlphaPreviewHalf;   // ColorEdit, ColorPicker, ColorButton: display half opaque / half transparent preview.
			// User Options (right-click on widget to change some of them)
			// Current settings are stored in style.ColorEditFlags.
			public static int AlphaBar;   // ColorEdit, ColorPicker: show vertical alpha bar/gradient in picker.
			public static int HDR;   // (WIP) ColorEdit: Currently only disable 0.0f..1.0f limits in RGBA edition (note: you probably want to use ...
			public static int DisplayRGB;   // [Display] ColorEdit: override _display_ type among RGB/HSV/Hex. ColorPicker: select any combination using one ...
			public static int DisplayHSV;   // [Display] "
			public static int DisplayHex;   // [Display] "
			public static int Uint8;   // [DataType] ColorEdit, ColorPicker, ColorButton: _display_ values formatted as 0..255.
			public static int Float;   // [DataType] ColorEdit, ColorPicker, ColorButton: _display_ values formatted as 0.0f..1.0f floats instead of ...
			public static int PickerHueBar;   // [Picker] ColorPicker: bar for Hue, rectangle for Sat/Value.
			public static int PickerHueWheel;   // [Picker] ColorPicker: wheel for Hue, triangle for Sat/Value.
			public static int PickerNoRotate;   // [1.93 only] [Picker] ColorPicker: disable rotating Sat/Value triangle. Best set in io.ConfigColorEditFlags once.
			public static int InputRGB;   // [Input] ColorEdit, ColorPicker: input and output data in RGB format.
			public static int InputHSV;   // [Input] ColorEdit, ColorPicker: input and output data in HSV format.
			public static int DefaultOptions_;
			// [Internal] Masks
			public static int AlphaMask_;
			public static int DisplayMask_;
			public static int DataTypeMask_;
			public static int PickerMask_;
			public static int InputMask_;

			static ImGuiColorEditFlags() { Init(ImGuiVersion.Version, ImGuiVersion.Docking); }

			// 1.93 and later get 1.93's numbers; anything older gets 1.92.5's.
			public static void Init(int version, bool docking)
			{
				if (version >= 19300) Init193(docking);
				else Init1925(docking);
			}

			// Dear ImGui 1.93 (ImGuiColorEditFlags_ in imgui.h)
			static void Init193(bool docking)
			{
				None = 0;
				NoAlpha = 1 << 1;
				NoPicker = 1 << 2;
				NoOptions = 1 << 3;
				NoSmallPreview = 1 << 4;
				NoInputs = 1 << 5;
				NoTooltip = 1 << 6;
				NoLabel = 1 << 7;
				NoSidePreview = 1 << 8;
				NoDragDrop = 1 << 9;
				NoBorder = 1 << 10;
				NoColorMarkers = 1 << 11;
				AlphaOpaque = 1 << 12;
				AlphaNoBg = 1 << 13;
				AlphaPreviewHalf = 1 << 14;
				AlphaBar = 1 << 18;
				HDR = 1 << 19;
				DisplayRGB = 1 << 20;
				DisplayHSV = 1 << 21;
				DisplayHex = 1 << 22;
				Uint8 = 1 << 23;
				Float = 1 << 24;
				PickerHueBar = 1 << 25;
				PickerHueWheel = 1 << 26;
				PickerNoRotate = 1 << 27;
				InputRGB = 1 << 28;
				InputHSV = 1 << 29;
				DefaultOptions_ = Uint8 | DisplayRGB | InputRGB | PickerHueBar;
				AlphaMask_ = NoAlpha | AlphaOpaque | AlphaNoBg | AlphaPreviewHalf;
				DisplayMask_ = DisplayRGB | DisplayHSV | DisplayHex;
				DataTypeMask_ = Uint8 | Float;
				PickerMask_ = PickerHueWheel | PickerHueBar;
				InputMask_ = InputRGB | InputHSV;
			}

			// Dear ImGui 1.92.5 (ImGuiColorEditFlags_ in imgui.h)
			static void Init1925(bool docking)
			{
				// not in 1.92.5
				NoColorMarkers = 0;
				PickerNoRotate = 0;
				None = 0;
				NoAlpha = 1 << 1;
				NoPicker = 1 << 2;
				NoOptions = 1 << 3;
				NoSmallPreview = 1 << 4;
				NoInputs = 1 << 5;
				NoTooltip = 1 << 6;
				NoLabel = 1 << 7;
				NoSidePreview = 1 << 8;
				NoDragDrop = 1 << 9;
				NoBorder = 1 << 10;
				AlphaOpaque = 1 << 11;
				AlphaNoBg = 1 << 12;
				AlphaPreviewHalf = 1 << 13;
				AlphaBar = 1 << 16;
				HDR = 1 << 19;
				DisplayRGB = 1 << 20;
				DisplayHSV = 1 << 21;
				DisplayHex = 1 << 22;
				Uint8 = 1 << 23;
				Float = 1 << 24;
				PickerHueBar = 1 << 25;
				PickerHueWheel = 1 << 26;
				InputRGB = 1 << 27;
				InputHSV = 1 << 28;
				DefaultOptions_ = Uint8 | DisplayRGB | InputRGB | PickerHueBar;
				AlphaMask_ = NoAlpha | AlphaOpaque | AlphaNoBg | AlphaPreviewHalf;
				DisplayMask_ = DisplayRGB | DisplayHSV | DisplayHex;
				DataTypeMask_ = Uint8 | Float;
				PickerMask_ = PickerHueWheel | PickerHueBar;
				InputMask_ = InputRGB | InputHSV;
			}
		}

		public static class ImGuiPopupFlags
		{
			public static int None;
			public static int MouseButtonLeft;   // For BeginPopupContext*(): open on Left Mouse release. Only one button allowed!
			public static int MouseButtonRight;   // For BeginPopupContext*(): open on Right Mouse release. Only one button allowed! (default)
			public static int MouseButtonMiddle;   // For BeginPopupContext*(): open on Middle Mouse release. Only one button allowed!
			public static int NoReopen;   // For OpenPopup*(), BeginPopupContext*(): don't reopen same popup if already open (won't reposition, won't ...
			public static int NoOpenOverExistingPopup;   // For OpenPopup*(), BeginPopupContext*(): don't open if there's already a popup at the same level of the popup ...
			public static int NoOpenOverItems;   // For BeginPopupContextWindow(): don't return true when hovering items, only when hovering empty space
			public static int AnyPopupId;   // For IsPopupOpen(): ignore the ImGuiID parameter and test for any popup.
			public static int AnyPopupLevel;   // For IsPopupOpen(): search/test at any level of the popup stack (default test in the current level)
			public static int AnyPopup;
			public static int MouseButtonShift_;   // [1.93 only] [Internal]
			public static int MouseButtonMask_;   // [Internal]
			public static int InvalidMask_;   // [1.93 only] [Internal] Reserve legacy bits 0-1 to detect incorrectly passing 1 or 2 to the function.
			public static int MouseButtonDefault_;

			static ImGuiPopupFlags() { Init(ImGuiVersion.Version, ImGuiVersion.Docking); }

			// 1.93 and later get 1.93's numbers; anything older gets 1.92.5's.
			public static void Init(int version, bool docking)
			{
				if (version >= 19300) Init193(docking);
				else Init1925(docking);
			}

			// Dear ImGui 1.93 (ImGuiPopupFlags_ in imgui.h)
			static void Init193(bool docking)
			{
				None = 0;
				MouseButtonLeft = 1 << 2;
				MouseButtonRight = 2 << 2;
				MouseButtonMiddle = 3 << 2;
				NoReopen = 1 << 5;
				NoOpenOverExistingPopup = 1 << 7;
				NoOpenOverItems = 1 << 8;
				AnyPopupId = 1 << 10;
				AnyPopupLevel = 1 << 11;
				AnyPopup = AnyPopupId | AnyPopupLevel;
				MouseButtonShift_ = 2;
				MouseButtonMask_ = 0x0C;
				InvalidMask_ = 0x03;
				MouseButtonDefault_ = MouseButtonRight;
			}

			// Dear ImGui 1.92.5 (ImGuiPopupFlags_ in imgui.h)
			static void Init1925(bool docking)
			{
				// not in 1.92.5
				MouseButtonShift_ = 0;
				InvalidMask_ = 0;
				None = 0;
				MouseButtonLeft = 0;
				MouseButtonRight = 1;
				MouseButtonMiddle = 2;
				MouseButtonMask_ = 0x1F;
				MouseButtonDefault_ = 1;
				NoReopen = 1 << 5;
				NoOpenOverExistingPopup = 1 << 7;
				NoOpenOverItems = 1 << 8;
				AnyPopupId = 1 << 10;
				AnyPopupLevel = 1 << 11;
				AnyPopup = AnyPopupId | AnyPopupLevel;
			}
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

		public static class ImDrawFlags
		{
			public static int None;
			// Rounding for AddRectXXX(), PathRect()
			public static int RoundTopLeft;   // OK -- Round top-left corner only (when 'rounding > 0.0f', we default to all corners).
			public static int RoundTopRight;   // OK -- Round top-right corner only (when 'rounding > 0.0f', we default to all corners).
			public static int RoundBottomLeft;   // OK -- Round bottom-left corner only (when 'rounding > 0.0f', we default to all corners).
			public static int RoundBottomRight;   // OK -- Round bottom-right corner only (when 'rounding > 0.0f', we default to all corners).
			public static int RoundNone;   // OK -- Disable rounding even when 'rounding > 0.0f'. This value is NOT zero, it is NOT an implicit flag!
			public static int RoundAll;   // (Default!!)
			public static int RoundTop;
			public static int RoundBottom;
			public static int RoundLeft;
			public static int RoundRight;
			// Stroke Options
			public static int Closed;   // OK -- PathStroke(), AddPolyline(): specify that shape should be closed.
			public static int JoinMiter;   // [1.93 only] OK -- PathStroke(), AddPolyline(): use miter joins/corners only. This assumes that the input polyline does ...
			public static int CapSquare;   // [1.93 only] OK -- PathStroke(), AddPolyline(): use square cap line ends.
			// Stroke/Fill Anti-aliasing
			public static int AAFill;   // [1.93 only] OK* OK(1) Enable anti-aliasing for Filled shapes.
			public static int AALines;   // [1.93 only] OK* OK(1) Enable anti-aliasing for Strokes (lines, borders).
			public static int AALineEnds;   // [1.93 only] OK OK(0) Enable anti-aliasing for Strokes Ends. Requires AALines to also be enabled. Useful on thick strokes ...
			// Stroke Position relative to shape outline
			public static int StrokeInside;   // [1.93 only] OK -- Draw stroke inside of the shape outline (default for closed shapes and AddLineH, AddLineV functions)
			public static int StrokeCenter;   // [1.93 only] OK -- Draw stroke at the center of the shape outline (default for paths, bezier, and AddLine functions)
			public static int StrokeCenterBiased;   // [1.93 only] OK -- Draw stroke at the center of the shape outline, so that half thickness rounded down will be outside, ...
			public static int StrokeOutside;   // [1.93 only] OK -- Draw stroke outside of the shape outline
			public static int StrokeLegacy;   // [1.93 only] OK OK(0) Use legacy positioning + enable JoinMiter + disable AALineEnds. Must be all bits set.
			// Other Options
			public static int TextNoPixelSnap;   // [1.93 only] OK OK(0) Disable automatically snapping AddText() calls to pixel boundaries.
			public static int UseTexForRoundCorners;   // [1.93 only] -- OK(1) Enable using textures instead of strokes to draw rounded corners/circles where possible (faster). ...
			public static int UseVtxOffset;   // [1.93 only] -- OK(1) Can emit 'VtxOffset > 0' to allow large meshes with 16-bit ImDrawIdx. Used by default when ...
			public static int AllowTexForRoundCorners_;   // [1.93 only] -- OK(1) [Internal]
			// [Internal]
			public static int RoundMask_;   // [Internal]
			public static int AllowInPushScope_;   // [1.93 only] [Internal] Values allowed in PushDrawFlag() scope.
			public static int AllowInFrameScope_;   // [1.93 only]
			public static int StrokeMask_;   // [1.93 only] [Internal]
			public static int InvalidMask_;   // [1.93 only] [Internal] == 0x8000000F. Reserved to detect misuses.
			public static int RoundCornersTopLeft;   // AddRect(), AddRectFilled(), PathRect(): enable rounding top-left corner only (when rounding > 0.0f, we ...
			public static int RoundCornersTopRight;   // AddRect(), AddRectFilled(), PathRect(): enable rounding top-right corner only (when rounding > 0.0f, we ...
			public static int RoundCornersBottomLeft;   // AddRect(), AddRectFilled(), PathRect(): enable rounding bottom-left corner only (when rounding > 0.0f, we ...
			public static int RoundCornersBottomRight;   // AddRect(), AddRectFilled(), PathRect(): enable rounding bottom-right corner only (when rounding > 0.0f, we ...
			public static int RoundCornersNone;   // AddRect(), AddRectFilled(), PathRect(): disable rounding on all corners (when rounding > 0.0f). This is NOT ...
			public static int RoundCornersAll;
			public static int RoundCornersTop;
			public static int RoundCornersBottom;
			public static int RoundCornersLeft;
			public static int RoundCornersRight;
			public static int RoundCornersMask_;
			public static int RoundCornersDefault_;   // Default to ALL corners if none of the _RoundCornersXX flags are specified.

			static ImDrawFlags() { Init(ImGuiVersion.Version, ImGuiVersion.Docking); }

			// 1.93 and later get 1.93's numbers; anything older gets 1.92.5's.
			public static void Init(int version, bool docking)
			{
				if (version >= 19300) Init193(docking);
				else Init1925(docking);
			}

			// Dear ImGui 1.93 (ImDrawFlags_ in imgui.h)
			static void Init193(bool docking)
			{
				None = 0;
				RoundTopLeft = 1 << 4;
				RoundTopRight = 1 << 5;
				RoundBottomLeft = 1 << 6;
				RoundBottomRight = 1 << 7;
				RoundNone = 1 << 8;
				RoundAll = RoundTopLeft | RoundTopRight | RoundBottomLeft | RoundBottomRight;
				RoundTop = RoundTopLeft | RoundTopRight;
				RoundBottom = RoundBottomLeft | RoundBottomRight;
				RoundLeft = RoundBottomLeft | RoundTopLeft;
				RoundRight = RoundBottomRight | RoundTopRight;
				Closed = 1 << 9;
				JoinMiter = 1 << 10;
				CapSquare = 1 << 11;
				AAFill = 1 << 12;
				AALines = 1 << 13;
				AALineEnds = 1 << 14;
				StrokeInside = 1 << 17;
				StrokeCenter = 2 << 17;
				StrokeCenterBiased = 3 << 17;
				StrokeOutside = 4 << 17;
				StrokeLegacy = 7 << 17;
				TextNoPixelSnap = 1 << 20;
				UseTexForRoundCorners = 1 << 21;
				UseVtxOffset = 1 << 22;
				AllowTexForRoundCorners_ = 1 << 23;
				RoundMask_ = RoundAll | RoundNone;
				AllowInPushScope_ = AAFill | AALines | AALineEnds | StrokeLegacy | TextNoPixelSnap | UseTexForRoundCorners | UseVtxOffset | RoundMask_;
				AllowInFrameScope_ = AllowInPushScope_ | AllowTexForRoundCorners_;
				StrokeMask_ = 0x07 << 17;
				InvalidMask_ = ~0x7FFFFFF0;
				RoundCornersTopLeft = RoundTopLeft;
				RoundCornersTopRight = RoundTopRight;
				RoundCornersBottomLeft = RoundBottomLeft;
				RoundCornersBottomRight = RoundBottomRight;
				RoundCornersNone = RoundNone;
				RoundCornersAll = RoundAll;
				RoundCornersTop = RoundTop;
				RoundCornersBottom = RoundBottom;
				RoundCornersLeft = RoundLeft;
				RoundCornersRight = RoundRight;
				RoundCornersMask_ = RoundMask_;
				RoundCornersDefault_ = RoundAll;
			}

			// Dear ImGui 1.92.5 (ImDrawFlags_ in imgui.h)
			static void Init1925(bool docking)
			{
				// not in 1.92.5
				JoinMiter = 0;
				CapSquare = 0;
				AAFill = 0;
				AALines = 0;
				AALineEnds = 0;
				StrokeInside = 0;
				StrokeCenter = 0;
				StrokeCenterBiased = 0;
				StrokeOutside = 0;
				StrokeLegacy = 0;
				TextNoPixelSnap = 0;
				UseTexForRoundCorners = 0;
				UseVtxOffset = 0;
				AllowTexForRoundCorners_ = 0;
				AllowInPushScope_ = 0;
				AllowInFrameScope_ = 0;
				StrokeMask_ = 0;
				InvalidMask_ = 0;
				None = 0;
				Closed = 1 << 0;
				RoundCornersTopLeft = 1 << 4;
				RoundCornersTopRight = 1 << 5;
				RoundCornersBottomLeft = 1 << 6;
				RoundCornersBottomRight = 1 << 7;
				RoundCornersNone = 1 << 8;
				RoundCornersTop = RoundCornersTopLeft | RoundCornersTopRight;
				RoundCornersBottom = RoundCornersBottomLeft | RoundCornersBottomRight;
				RoundCornersLeft = RoundCornersBottomLeft | RoundCornersTopLeft;
				RoundCornersRight = RoundCornersBottomRight | RoundCornersTopRight;
				RoundCornersAll = RoundCornersTopLeft | RoundCornersTopRight | RoundCornersBottomLeft | RoundCornersBottomRight;
				RoundCornersDefault_ = RoundCornersAll;
				RoundCornersMask_ = RoundCornersAll | RoundCornersNone;
				RoundTopLeft = RoundCornersTopLeft;
				RoundTopRight = RoundCornersTopRight;
				RoundBottomLeft = RoundCornersBottomLeft;
				RoundBottomRight = RoundCornersBottomRight;
				RoundNone = RoundCornersNone;
				RoundAll = RoundCornersAll;
				RoundTop = RoundCornersTop;
				RoundBottom = RoundCornersBottom;
				RoundLeft = RoundCornersLeft;
				RoundRight = RoundCornersRight;
				RoundMask_ = RoundCornersMask_;
			}
		}

		public static class ImGuiWindowFlags
		{
			public static int ImGuiWindowFlags_None;
			public static int ImGuiWindowFlags_NoTitleBar;   // Disable title-bar
			public static int ImGuiWindowFlags_NoResize;   // Disable user resizing with the lower-right grip
			public static int ImGuiWindowFlags_NoMove;   // Disable user moving the window
			public static int ImGuiWindowFlags_NoScrollbar;   // Disable scrollbars (window can still scroll with mouse or programmatically)
			public static int ImGuiWindowFlags_NoScrollWithMouse;   // Disable user vertically scrolling with mouse wheel. On child window, mouse wheel will be forwarded to the ...
			public static int ImGuiWindowFlags_NoCollapse;   // Disable user collapsing window by double-clicking on it. Also referred to as Window Menu Button (e.g. within ...
			public static int ImGuiWindowFlags_AlwaysAutoResize;   // Resize every window to its content every frame
			public static int ImGuiWindowFlags_NoBackground;   // Disable drawing background color (WindowBg, etc.) and outside border. Similar as using ...
			public static int ImGuiWindowFlags_NoSavedSettings;   // Never load/save settings in .ini file
			public static int ImGuiWindowFlags_NoMouseInputs;   // Disable catching mouse, hovering test with pass through.
			public static int ImGuiWindowFlags_MenuBar;   // Has a menu-bar
			public static int ImGuiWindowFlags_HorizontalScrollbar;   // Allow horizontal scrollbar to appear (off by default). You may use ...
			public static int ImGuiWindowFlags_NoFocusOnAppearing;   // Disable taking focus when transitioning from hidden to visible state
			public static int ImGuiWindowFlags_NoBringToFrontOnFocus;   // Disable bringing window to front when taking focus (e.g. clicking on it or programmatically giving it focus)
			public static int ImGuiWindowFlags_AlwaysVerticalScrollbar;   // Always show vertical scrollbar (even if ContentSize.y < Size.y)
			public static int ImGuiWindowFlags_AlwaysHorizontalScrollbar;   // Always show horizontal scrollbar (even if ContentSize.x < Size.x)
			public static int ImGuiWindowFlags_NoNavInputs;   // No keyboard/gamepad navigation within the window
			public static int ImGuiWindowFlags_NoNavFocus;   // No focusing toward this window with keyboard/gamepad navigation (e.g. skipped by Ctrl+Tab)
			public static int ImGuiWindowFlags_UnsavedDocument;   // Display a dot next to the title. When used in a tab/docking context, tab is selected when clicking the X + ...
			public static int ImGuiWindowFlags_NoDocking;   // [docking branch only] Disable docking of this window
			public static int ImGuiWindowFlags_NoNav;
			public static int ImGuiWindowFlags_NoDecoration;
			public static int ImGuiWindowFlags_NoInputs;
			// [Internal]
			public static int ImGuiWindowFlags_ChildWindow;   // Don't use! For internal use by BeginChild()
			public static int ImGuiWindowFlags_Tooltip;   // Don't use! For internal use by BeginTooltip()
			public static int ImGuiWindowFlags_Popup;   // Don't use! For internal use by BeginPopup()
			public static int ImGuiWindowFlags_Modal;   // Don't use! For internal use by BeginPopupModal()
			public static int ImGuiWindowFlags_ChildMenu;   // Don't use! For internal use by BeginMenu()
			public static int ImGuiWindowFlags_DockNodeHost;   // [docking branch only] Don't use! For internal use by Begin()/NewFrame()

			static ImGuiWindowFlags() { Init(ImGuiVersion.Version, ImGuiVersion.Docking); }

			// 1.93 and later get 1.93's numbers; anything older gets 1.92.5's.
			public static void Init(int version, bool docking)
			{
				if (version >= 19300) Init193(docking);
				else Init1925(docking);
			}

			// Dear ImGui 1.93 (ImGuiWindowFlags_ in imgui.h)
			static void Init193(bool docking)
			{
				ImGuiWindowFlags_None = 0;
				ImGuiWindowFlags_NoTitleBar = 1 << 0;
				ImGuiWindowFlags_NoResize = 1 << 1;
				ImGuiWindowFlags_NoMove = 1 << 2;
				ImGuiWindowFlags_NoScrollbar = 1 << 3;
				ImGuiWindowFlags_NoScrollWithMouse = 1 << 4;
				ImGuiWindowFlags_NoCollapse = 1 << 5;
				ImGuiWindowFlags_AlwaysAutoResize = 1 << 6;
				ImGuiWindowFlags_NoBackground = 1 << 7;
				ImGuiWindowFlags_NoSavedSettings = 1 << 8;
				ImGuiWindowFlags_NoMouseInputs = 1 << 9;
				ImGuiWindowFlags_MenuBar = 1 << 10;
				ImGuiWindowFlags_HorizontalScrollbar = 1 << 11;
				ImGuiWindowFlags_NoFocusOnAppearing = 1 << 12;
				ImGuiWindowFlags_NoBringToFrontOnFocus = 1 << 13;
				ImGuiWindowFlags_AlwaysVerticalScrollbar = 1 << 14;
				ImGuiWindowFlags_AlwaysHorizontalScrollbar = 1 << 15;
				ImGuiWindowFlags_NoNavInputs = 1 << 16;
				ImGuiWindowFlags_NoNavFocus = 1 << 17;
				ImGuiWindowFlags_UnsavedDocument = 1 << 18;
				ImGuiWindowFlags_NoDocking = docking ? 1 << 19 : 0;
				ImGuiWindowFlags_NoNav = ImGuiWindowFlags_NoNavInputs | ImGuiWindowFlags_NoNavFocus;
				ImGuiWindowFlags_NoDecoration = ImGuiWindowFlags_NoTitleBar | ImGuiWindowFlags_NoResize | ImGuiWindowFlags_NoScrollbar | ImGuiWindowFlags_NoCollapse;
				ImGuiWindowFlags_NoInputs = ImGuiWindowFlags_NoMouseInputs | ImGuiWindowFlags_NoNavInputs | ImGuiWindowFlags_NoNavFocus;
				ImGuiWindowFlags_ChildWindow = 1 << 24;
				ImGuiWindowFlags_Tooltip = 1 << 25;
				ImGuiWindowFlags_Popup = 1 << 26;
				ImGuiWindowFlags_Modal = 1 << 27;
				ImGuiWindowFlags_ChildMenu = 1 << 28;
				ImGuiWindowFlags_DockNodeHost = docking ? 1 << 23 : 0;
			}

			// Dear ImGui 1.92.5 (ImGuiWindowFlags_ in imgui.h)
			static void Init1925(bool docking)
			{
				ImGuiWindowFlags_None = 0;
				ImGuiWindowFlags_NoTitleBar = 1 << 0;
				ImGuiWindowFlags_NoResize = 1 << 1;
				ImGuiWindowFlags_NoMove = 1 << 2;
				ImGuiWindowFlags_NoScrollbar = 1 << 3;
				ImGuiWindowFlags_NoScrollWithMouse = 1 << 4;
				ImGuiWindowFlags_NoCollapse = 1 << 5;
				ImGuiWindowFlags_AlwaysAutoResize = 1 << 6;
				ImGuiWindowFlags_NoBackground = 1 << 7;
				ImGuiWindowFlags_NoSavedSettings = 1 << 8;
				ImGuiWindowFlags_NoMouseInputs = 1 << 9;
				ImGuiWindowFlags_MenuBar = 1 << 10;
				ImGuiWindowFlags_HorizontalScrollbar = 1 << 11;
				ImGuiWindowFlags_NoFocusOnAppearing = 1 << 12;
				ImGuiWindowFlags_NoBringToFrontOnFocus = 1 << 13;
				ImGuiWindowFlags_AlwaysVerticalScrollbar = 1 << 14;
				ImGuiWindowFlags_AlwaysHorizontalScrollbar = 1 << 15;
				ImGuiWindowFlags_NoNavInputs = 1 << 16;
				ImGuiWindowFlags_NoNavFocus = 1 << 17;
				ImGuiWindowFlags_UnsavedDocument = 1 << 18;
				ImGuiWindowFlags_NoDocking = docking ? 1 << 19 : 0;
				ImGuiWindowFlags_NoNav = ImGuiWindowFlags_NoNavInputs | ImGuiWindowFlags_NoNavFocus;
				ImGuiWindowFlags_NoDecoration = ImGuiWindowFlags_NoTitleBar | ImGuiWindowFlags_NoResize | ImGuiWindowFlags_NoScrollbar | ImGuiWindowFlags_NoCollapse;
				ImGuiWindowFlags_NoInputs = ImGuiWindowFlags_NoMouseInputs | ImGuiWindowFlags_NoNavInputs | ImGuiWindowFlags_NoNavFocus;
				ImGuiWindowFlags_DockNodeHost = docking ? 1 << 23 : 0;
				ImGuiWindowFlags_ChildWindow = 1 << 24;
				ImGuiWindowFlags_Tooltip = 1 << 25;
				ImGuiWindowFlags_Popup = 1 << 26;
				ImGuiWindowFlags_Modal = 1 << 27;
				ImGuiWindowFlags_ChildMenu = 1 << 28;
			}
		}

		public static class ImGuiMouseButton
		{
			public static int Left;
			public static int Right;
			public static int Middle;
			public static int COUNT;

			static ImGuiMouseButton() { Init(ImGuiVersion.Version, ImGuiVersion.Docking); }

			// 1.93 and later get 1.93's numbers; anything older gets 1.92.5's.
			public static void Init(int version, bool docking)
			{
				if (version >= 19300) Init193(docking);
				else Init1925(docking);
			}

			// Dear ImGui 1.93 (ImGuiMouseButton_ in imgui.h)
			static void Init193(bool docking)
			{
				Left = 0;
				Right = 1;
				Middle = 2;
				COUNT = 5;
			}

			// Dear ImGui 1.92.5 (ImGuiMouseButton_ in imgui.h)
			static void Init1925(bool docking)
			{
				Left = 0;
				Right = 1;
				Middle = 2;
				COUNT = 5;
			}
		}

		public static class ImGuiSelectableFlags
		{
			public static int ImGuiSelectableFlags_None;
			public static int ImGuiSelectableFlags_NoAutoClosePopups;   // Clicking this doesn't close parent popup window (overrides ImGuiItemFlags_AutoClosePopups)
			public static int ImGuiSelectableFlags_SpanAllColumns;   // Frame will span all columns of its container table (text will still fit in current column)
			public static int ImGuiSelectableFlags_AllowDoubleClick;   // Generate press events on double clicks too
			public static int ImGuiSelectableFlags_Disabled;   // Cannot be selected, display grayed out text
			public static int ImGuiSelectableFlags_AllowOverlap;   // Hit testing will allow subsequent widgets to overlap this one. Require previous frame HoveredId to match ...
			public static int ImGuiSelectableFlags_Highlight;   // Make the item be displayed as if it is hovered
			public static int ImGuiSelectableFlags_SelectOnNav;   // Auto-select when moved into, unless Ctrl is held. Automatic when in a BeginMultiSelect() block.
			public static int ImGuiSelectableFlags_DontClosePopups;   // Renamed in 1.91.0

			static ImGuiSelectableFlags() { Init(ImGuiVersion.Version, ImGuiVersion.Docking); }

			// 1.93 and later get 1.93's numbers; anything older gets 1.92.5's.
			public static void Init(int version, bool docking)
			{
				if (version >= 19300) Init193(docking);
				else Init1925(docking);
			}

			// Dear ImGui 1.93 (ImGuiSelectableFlags_ in imgui.h)
			static void Init193(bool docking)
			{
				ImGuiSelectableFlags_None = 0;
				ImGuiSelectableFlags_NoAutoClosePopups = 1 << 0;
				ImGuiSelectableFlags_SpanAllColumns = 1 << 1;
				ImGuiSelectableFlags_AllowDoubleClick = 1 << 2;
				ImGuiSelectableFlags_Disabled = 1 << 3;
				ImGuiSelectableFlags_AllowOverlap = 1 << 4;
				ImGuiSelectableFlags_Highlight = 1 << 5;
				ImGuiSelectableFlags_SelectOnNav = 1 << 6;
				ImGuiSelectableFlags_DontClosePopups = ImGuiSelectableFlags_NoAutoClosePopups;
			}

			// Dear ImGui 1.92.5 (ImGuiSelectableFlags_ in imgui.h)
			static void Init1925(bool docking)
			{
				ImGuiSelectableFlags_None = 0;
				ImGuiSelectableFlags_NoAutoClosePopups = 1 << 0;
				ImGuiSelectableFlags_SpanAllColumns = 1 << 1;
				ImGuiSelectableFlags_AllowDoubleClick = 1 << 2;
				ImGuiSelectableFlags_Disabled = 1 << 3;
				ImGuiSelectableFlags_AllowOverlap = 1 << 4;
				ImGuiSelectableFlags_Highlight = 1 << 5;
				ImGuiSelectableFlags_SelectOnNav = 1 << 6;
				ImGuiSelectableFlags_DontClosePopups = ImGuiSelectableFlags_NoAutoClosePopups;
			}
		}

		public static class ImGuiStyleVar
		{
			// Enum name
			public static int Alpha;   // float Alpha
			public static int DisabledAlpha;   // float DisabledAlpha
			public static int WindowPadding;   // ImVec2 WindowPadding
			public static int WindowRounding;   // float WindowRounding
			public static int WindowBorderSize;   // float WindowBorderSize
			public static int WindowMinSize;   // ImVec2 WindowMinSize
			public static int WindowTitleAlign;   // ImVec2 WindowTitleAlign
			public static int ChildRounding;   // float ChildRounding
			public static int ChildBorderSize;   // float ChildBorderSize
			public static int PopupRounding;   // float PopupRounding
			public static int PopupBorderSize;   // float PopupBorderSize
			public static int FramePadding;   // ImVec2 FramePadding
			public static int FrameRounding;   // float FrameRounding
			public static int FrameBorderSize;   // float FrameBorderSize
			public static int ItemSpacing;   // ImVec2 ItemSpacing
			public static int ItemInnerSpacing;   // ImVec2 ItemInnerSpacing
			public static int IndentSpacing;   // float IndentSpacing
			public static int CellPadding;   // ImVec2 CellPadding
			public static int ScrollbarSize;   // float ScrollbarSize
			public static int ScrollbarRounding;   // float ScrollbarRounding
			public static int ScrollbarPadding;   // float ScrollbarPadding
			public static int GrabMinSize;   // float GrabMinSize
			public static int GrabRounding;   // float GrabRounding
			public static int LayoutAlign;   // [1.92.5 only] float LayoutAlign
			public static int ImageRounding;   // [1.93 only] float ImageRounding
			public static int ImageBorderSize;   // float ImageBorderSize
			public static int TabRounding;   // float TabRounding
			public static int TabBorderSize;   // float TabBorderSize
			public static int TabMinWidthBase;   // float TabMinWidthBase
			public static int TabMinWidthShrink;   // float TabMinWidthShrink
			public static int TabBarBorderSize;   // float TabBarBorderSize
			public static int TabBarOverlineSize;   // float TabBarOverlineSize
			public static int TableAngledHeadersAngle;   // float TableAngledHeadersAngle
			public static int TableAngledHeadersTextAlign;   // ImVec2 TableAngledHeadersTextAlign
			public static int TreeLinesSize;   // float TreeLinesSize
			public static int TreeLinesRounding;   // float TreeLinesRounding
			public static int MenuItemRounding;   // [1.93 only] float MenuItemRounding
			public static int SelectableRounding;   // [1.93 only] float SelectableRounding
			public static int DragDropTargetRounding;   // [1.93 only] float DragDropTargetRounding
			public static int ButtonTextAlign;   // ImVec2 ButtonTextAlign
			public static int SelectableTextAlign;   // ImVec2 SelectableTextAlign
			public static int SeparatorSize;   // [1.93 only] float SeparatorSize
			public static int SeparatorTextBorderSize;   // float SeparatorTextBorderSize
			public static int SeparatorTextAlign;   // ImVec2 SeparatorTextAlign
			public static int SeparatorTextPadding;   // ImVec2 SeparatorTextPadding
			public static int DockingSeparatorSize;   // [docking branch only] float DockingSeparatorSize
			public static int COUNT;

			static ImGuiStyleVar() { Init(ImGuiVersion.Version, ImGuiVersion.Docking); }

			// 1.93 and later get 1.93's numbers; anything older gets 1.92.5's.
			public static void Init(int version, bool docking)
			{
				if (version >= 19300) Init193(docking);
				else Init1925(docking);
			}

			// Dear ImGui 1.93 (ImGuiStyleVar_ in imgui.h)
			static void Init193(bool docking)
			{
				// not in 1.93
				LayoutAlign = -1;
				int i = 0;
				Alpha = i++;
				DisabledAlpha = i++;
				WindowPadding = i++;
				WindowRounding = i++;
				WindowBorderSize = i++;
				WindowMinSize = i++;
				WindowTitleAlign = i++;
				ChildRounding = i++;
				ChildBorderSize = i++;
				PopupRounding = i++;
				PopupBorderSize = i++;
				FramePadding = i++;
				FrameRounding = i++;
				FrameBorderSize = i++;
				ItemSpacing = i++;
				ItemInnerSpacing = i++;
				IndentSpacing = i++;
				CellPadding = i++;
				ScrollbarSize = i++;
				ScrollbarRounding = i++;
				ScrollbarPadding = i++;
				GrabMinSize = i++;
				GrabRounding = i++;
				ImageRounding = i++;
				ImageBorderSize = i++;
				TabRounding = i++;
				TabBorderSize = i++;
				TabMinWidthBase = i++;
				TabMinWidthShrink = i++;
				TabBarBorderSize = i++;
				TabBarOverlineSize = i++;
				TableAngledHeadersAngle = i++;
				TableAngledHeadersTextAlign = i++;
				TreeLinesSize = i++;
				TreeLinesRounding = i++;
				MenuItemRounding = i++;
				SelectableRounding = i++;
				DragDropTargetRounding = i++;
				ButtonTextAlign = i++;
				SelectableTextAlign = i++;
				SeparatorSize = i++;
				SeparatorTextBorderSize = i++;
				SeparatorTextAlign = i++;
				SeparatorTextPadding = i++;
				DockingSeparatorSize = docking ? i++ : -1;
				COUNT = i++;
			}

			// Dear ImGui 1.92.5 (ImGuiStyleVar_ in imgui.h)
			static void Init1925(bool docking)
			{
				// not in 1.92.5
				ImageRounding = -1;
				MenuItemRounding = -1;
				SelectableRounding = -1;
				DragDropTargetRounding = -1;
				SeparatorSize = -1;
				int i = 0;
				Alpha = i++;
				DisabledAlpha = i++;
				WindowPadding = i++;
				WindowRounding = i++;
				WindowBorderSize = i++;
				WindowMinSize = i++;
				WindowTitleAlign = i++;
				ChildRounding = i++;
				ChildBorderSize = i++;
				PopupRounding = i++;
				PopupBorderSize = i++;
				FramePadding = i++;
				FrameRounding = i++;
				FrameBorderSize = i++;
				ItemSpacing = i++;
				ItemInnerSpacing = i++;
				IndentSpacing = i++;
				CellPadding = i++;
				ScrollbarSize = i++;
				ScrollbarRounding = i++;
				ScrollbarPadding = i++;
				GrabMinSize = i++;
				GrabRounding = i++;
				LayoutAlign = i++;
				ImageBorderSize = i++;
				TabRounding = i++;
				TabBorderSize = i++;
				TabMinWidthBase = i++;
				TabMinWidthShrink = i++;
				TabBarBorderSize = i++;
				TabBarOverlineSize = i++;
				TableAngledHeadersAngle = i++;
				TableAngledHeadersTextAlign = i++;
				TreeLinesSize = i++;
				TreeLinesRounding = i++;
				ButtonTextAlign = i++;
				SelectableTextAlign = i++;
				SeparatorTextBorderSize = i++;
				SeparatorTextAlign = i++;
				SeparatorTextPadding = i++;
				DockingSeparatorSize = docking ? i++ : -1;
				COUNT = i++;
			}
		}

		public static class ImGuiChildFlags
		{
			public static int None;
			public static int Borders;   // Show an outer border and enable WindowPadding. (IMPORTANT: this is always == 1 == true for legacy reason)
			public static int AlwaysUseWindowPadding;   // Pad with style.WindowPadding even if no border are drawn (no padding by default for non-bordered child ...
			public static int ResizeX;   // Allow resize from right border (layout direction). Enable .ini saving (unless ...
			public static int ResizeY;   // Allow resize from bottom border (layout direction). "
			public static int AutoResizeX;   // Enable auto-resizing width. Read "IMPORTANT: Size measurement" details above.
			public static int AutoResizeY;   // Enable auto-resizing height. Read "IMPORTANT: Size measurement" details above.
			public static int AlwaysAutoResize;   // Combined with AutoResizeX/AutoResizeY. Always measure size even when child is hidden, always return true, ...
			public static int FrameStyle;   // Style the child window like a framed item: use FrameBg, FrameRounding, FrameBorderSize, FramePadding instead ...
			public static int NavFlattened;   // [BETA] Share focus scope, allow keyboard/gamepad navigation to cross over parent border to this child or ...

			static ImGuiChildFlags() { Init(ImGuiVersion.Version, ImGuiVersion.Docking); }

			// 1.93 and later get 1.93's numbers; anything older gets 1.92.5's.
			public static void Init(int version, bool docking)
			{
				if (version >= 19300) Init193(docking);
				else Init1925(docking);
			}

			// Dear ImGui 1.93 (ImGuiChildFlags_ in imgui.h)
			static void Init193(bool docking)
			{
				None = 0;
				Borders = 1 << 0;
				AlwaysUseWindowPadding = 1 << 1;
				ResizeX = 1 << 2;
				ResizeY = 1 << 3;
				AutoResizeX = 1 << 4;
				AutoResizeY = 1 << 5;
				AlwaysAutoResize = 1 << 6;
				FrameStyle = 1 << 7;
				NavFlattened = 1 << 8;
			}

			// Dear ImGui 1.92.5 (ImGuiChildFlags_ in imgui.h)
			static void Init1925(bool docking)
			{
				None = 0;
				Borders = 1 << 0;
				AlwaysUseWindowPadding = 1 << 1;
				ResizeX = 1 << 2;
				ResizeY = 1 << 3;
				AutoResizeX = 1 << 4;
				AutoResizeY = 1 << 5;
				AlwaysAutoResize = 1 << 6;
				FrameStyle = 1 << 7;
				NavFlattened = 1 << 8;
			}
		}

		public static class ImGuiCol
		{
			public static int Text;
			public static int TextDisabled;
			public static int WindowBg;   // Background of normal windows
			public static int ChildBg;   // Background of child windows
			public static int PopupBg;   // Background of popups, menus, tooltips windows
			public static int Border;
			public static int BorderShadow;
			public static int FrameBg;   // Background of checkbox, radio button, plot, slider, text input
			public static int FrameBgHovered;
			public static int FrameBgActive;
			public static int TitleBg;   // Title bar
			public static int TitleBgActive;   // Title bar when focused
			public static int TitleBgCollapsed;   // Title bar when collapsed
			public static int MenuBarBg;
			public static int ScrollbarBg;
			public static int ScrollbarGrab;
			public static int ScrollbarGrabHovered;
			public static int ScrollbarGrabActive;
			public static int CheckMark;   // Checkbox tick and RadioButton circle
			public static int CheckboxSelectedBg;   // [1.93 only] Checkbox background when Selected, otherwise use FrameBg
			public static int SliderGrab;
			public static int SliderGrabActive;
			public static int Button;
			public static int ButtonHovered;
			public static int ButtonActive;
			public static int Header;   // Header* colors are used for CollapsingHeader, TreeNode, Selectable, MenuItem
			public static int HeaderHovered;
			public static int HeaderActive;
			public static int Separator;
			public static int SeparatorHovered;
			public static int SeparatorActive;
			public static int ResizeGrip;   // Resize grip in lower-right and lower-left corners of windows.
			public static int ResizeGripHovered;
			public static int ResizeGripActive;
			public static int InputTextCursor;   // InputText cursor/caret
			public static int TabHovered;   // Tab background, when hovered
			public static int Tab;   // Tab background, when tab-bar is focused & tab is unselected
			public static int TabSelected;   // Tab background, when tab-bar is focused & tab is selected
			public static int TabSelectedOverline;   // Tab horizontal overline, when tab-bar is focused & tab is selected
			public static int TabDimmed;   // Tab background, when tab-bar is unfocused & tab is unselected
			public static int TabDimmedSelected;   // Tab background, when tab-bar is unfocused & tab is selected
			public static int TabDimmedSelectedOverline;   // ..horizontal overline, when tab-bar is unfocused & tab is selected
			public static int DockingPreview;   // [docking branch only] Preview overlay color when about to docking something
			public static int DockingEmptyBg;   // [docking branch only] Background color for empty node (e.g. CentralNode with no window docked into it)
			public static int PlotLines;
			public static int PlotLinesHovered;
			public static int PlotHistogram;
			public static int PlotHistogramHovered;
			public static int TableHeaderBg;   // Table header background
			public static int TableBorderStrong;   // Table outer and header borders (prefer using Alpha=1.0 here)
			public static int TableBorderLight;   // Table inner borders (prefer using Alpha=1.0 here)
			public static int TableRowBg;   // Table row background (even rows)
			public static int TableRowBgAlt;   // Table row background (odd rows)
			public static int TextLink;   // Hyperlink color
			public static int TextSelectedBg;   // Selected text inside an InputText
			public static int TreeLines;   // Tree node hierarchy outlines when using ImGuiTreeNodeFlags_DrawLines
			public static int DragDropTarget;   // Rectangle border highlighting a drop target
			public static int DragDropTargetBg;   // Rectangle background highlighting a drop target
			public static int UnsavedMarker;   // Unsaved Document marker (in window title and tabs)
			public static int NavCursor;   // Color of keyboard/gamepad navigation cursor/rectangle, when visible
			public static int NavWindowingHighlight;   // Highlight window when using Ctrl+Tab
			public static int NavWindowingDimBg;   // Darken/colorize entire screen behind the Ctrl+Tab window list, when active
			public static int ModalWindowDimBg;   // Darken/colorize entire screen behind a modal window, when one is active
			public static int COUNT;
			public static int TabActive;   // [renamed in 1.90.9]
			public static int TabUnfocused;   // [renamed in 1.90.9]
			public static int TabUnfocusedActive;   // [renamed in 1.90.9]
			public static int NavHighlight;   // [renamed in 1.91.4]

			static ImGuiCol() { Init(ImGuiVersion.Version, ImGuiVersion.Docking); }

			// 1.93 and later get 1.93's numbers; anything older gets 1.92.5's.
			public static void Init(int version, bool docking)
			{
				if (version >= 19300) Init193(docking);
				else Init1925(docking);
			}

			// Dear ImGui 1.93 (ImGuiCol_ in imgui.h)
			static void Init193(bool docking)
			{
				int i = 0;
				Text = i++;
				TextDisabled = i++;
				WindowBg = i++;
				ChildBg = i++;
				PopupBg = i++;
				Border = i++;
				BorderShadow = i++;
				FrameBg = i++;
				FrameBgHovered = i++;
				FrameBgActive = i++;
				TitleBg = i++;
				TitleBgActive = i++;
				TitleBgCollapsed = i++;
				MenuBarBg = i++;
				ScrollbarBg = i++;
				ScrollbarGrab = i++;
				ScrollbarGrabHovered = i++;
				ScrollbarGrabActive = i++;
				CheckMark = i++;
				CheckboxSelectedBg = i++;
				SliderGrab = i++;
				SliderGrabActive = i++;
				Button = i++;
				ButtonHovered = i++;
				ButtonActive = i++;
				Header = i++;
				HeaderHovered = i++;
				HeaderActive = i++;
				Separator = i++;
				SeparatorHovered = i++;
				SeparatorActive = i++;
				ResizeGrip = i++;
				ResizeGripHovered = i++;
				ResizeGripActive = i++;
				InputTextCursor = i++;
				TabHovered = i++;
				Tab = i++;
				TabSelected = i++;
				TabSelectedOverline = i++;
				TabDimmed = i++;
				TabDimmedSelected = i++;
				TabDimmedSelectedOverline = i++;
				DockingPreview = docking ? i++ : -1;
				DockingEmptyBg = docking ? i++ : -1;
				PlotLines = i++;
				PlotLinesHovered = i++;
				PlotHistogram = i++;
				PlotHistogramHovered = i++;
				TableHeaderBg = i++;
				TableBorderStrong = i++;
				TableBorderLight = i++;
				TableRowBg = i++;
				TableRowBgAlt = i++;
				TextLink = i++;
				TextSelectedBg = i++;
				TreeLines = i++;
				DragDropTarget = i++;
				DragDropTargetBg = i++;
				UnsavedMarker = i++;
				NavCursor = i++;
				NavWindowingHighlight = i++;
				NavWindowingDimBg = i++;
				ModalWindowDimBg = i++;
				COUNT = i++;
				TabActive = TabSelected;
				TabUnfocused = TabDimmed;
				TabUnfocusedActive = TabDimmedSelected;
				NavHighlight = NavCursor;
			}

			// Dear ImGui 1.92.5 (ImGuiCol_ in imgui.h)
			static void Init1925(bool docking)
			{
				// not in 1.92.5
				CheckboxSelectedBg = -1;
				int i = 0;
				Text = i++;
				TextDisabled = i++;
				WindowBg = i++;
				ChildBg = i++;
				PopupBg = i++;
				Border = i++;
				BorderShadow = i++;
				FrameBg = i++;
				FrameBgHovered = i++;
				FrameBgActive = i++;
				TitleBg = i++;
				TitleBgActive = i++;
				TitleBgCollapsed = i++;
				MenuBarBg = i++;
				ScrollbarBg = i++;
				ScrollbarGrab = i++;
				ScrollbarGrabHovered = i++;
				ScrollbarGrabActive = i++;
				CheckMark = i++;
				SliderGrab = i++;
				SliderGrabActive = i++;
				Button = i++;
				ButtonHovered = i++;
				ButtonActive = i++;
				Header = i++;
				HeaderHovered = i++;
				HeaderActive = i++;
				Separator = i++;
				SeparatorHovered = i++;
				SeparatorActive = i++;
				ResizeGrip = i++;
				ResizeGripHovered = i++;
				ResizeGripActive = i++;
				InputTextCursor = i++;
				TabHovered = i++;
				Tab = i++;
				TabSelected = i++;
				TabSelectedOverline = i++;
				TabDimmed = i++;
				TabDimmedSelected = i++;
				TabDimmedSelectedOverline = i++;
				DockingPreview = docking ? i++ : -1;
				DockingEmptyBg = docking ? i++ : -1;
				PlotLines = i++;
				PlotLinesHovered = i++;
				PlotHistogram = i++;
				PlotHistogramHovered = i++;
				TableHeaderBg = i++;
				TableBorderStrong = i++;
				TableBorderLight = i++;
				TableRowBg = i++;
				TableRowBgAlt = i++;
				TextLink = i++;
				TextSelectedBg = i++;
				TreeLines = i++;
				DragDropTarget = i++;
				DragDropTargetBg = i++;
				UnsavedMarker = i++;
				NavCursor = i++;
				NavWindowingHighlight = i++;
				NavWindowingDimBg = i++;
				ModalWindowDimBg = i++;
				COUNT = i++;
				TabActive = TabSelected;
				TabUnfocused = TabDimmed;
				TabUnfocusedActive = TabDimmedSelected;
				NavHighlight = NavCursor;
			}
		}

		public static class ImGuiTableBgTarget
		{
			public static int None;
			public static int RowBg0;   // Set row background color 0 (generally used for background, automatically set when ImGuiTableFlags_RowBg is ...
			public static int RowBg1;   // Set row background color 1 (generally used for selection marking)
			public static int CellBg;   // Set cell background color (top-most color)

			static ImGuiTableBgTarget() { Init(ImGuiVersion.Version, ImGuiVersion.Docking); }

			// 1.93 and later get 1.93's numbers; anything older gets 1.92.5's.
			public static void Init(int version, bool docking)
			{
				if (version >= 19300) Init193(docking);
				else Init1925(docking);
			}

			// Dear ImGui 1.93 (ImGuiTableBgTarget_ in imgui.h)
			static void Init193(bool docking)
			{
				None = 0;
				RowBg0 = 1;
				RowBg1 = 2;
				CellBg = 3;
			}

			// Dear ImGui 1.92.5 (ImGuiTableBgTarget_ in imgui.h)
			static void Init1925(bool docking)
			{
				None = 0;
				RowBg0 = 1;
				RowBg1 = 2;
				CellBg = 3;
			}
		}

		public static class ImGuiCond
		{
			public static int None;   // No condition (always set the variable), same as _Always
			public static int Always;   // No condition (always set the variable), same as _None
			public static int Once;   // Set the variable once per runtime session (only the first call will succeed)
			public static int FirstUseEver;   // Set the variable if the object/window has no persistently saved data (no entry in .ini file)
			public static int Appearing;   // Set the variable if the object/window is appearing after being hidden/inactive (or the first time)

			static ImGuiCond() { Init(ImGuiVersion.Version, ImGuiVersion.Docking); }

			// 1.93 and later get 1.93's numbers; anything older gets 1.92.5's.
			public static void Init(int version, bool docking)
			{
				if (version >= 19300) Init193(docking);
				else Init1925(docking);
			}

			// Dear ImGui 1.93 (ImGuiCond_ in imgui.h)
			static void Init193(bool docking)
			{
				None = 0;
				Always = 1 << 0;
				Once = 1 << 1;
				FirstUseEver = 1 << 2;
				Appearing = 1 << 3;
			}

			// Dear ImGui 1.92.5 (ImGuiCond_ in imgui.h)
			static void Init1925(bool docking)
			{
				None = 0;
				Always = 1 << 0;
				Once = 1 << 1;
				FirstUseEver = 1 << 2;
				Appearing = 1 << 3;
			}
		}

		public static class ImGuiTreeNodeFlags
		{
			public static int ImGuiTreeNodeFlags_None;
			public static int ImGuiTreeNodeFlags_Selected;   // Draw as selected
			public static int ImGuiTreeNodeFlags_Framed;   // Draw frame with background (e.g. for CollapsingHeader)
			public static int ImGuiTreeNodeFlags_AllowOverlap;   // Hit testing will allow subsequent widgets to overlap this one. Require previous frame HoveredId to match ...
			public static int ImGuiTreeNodeFlags_NoTreePushOnOpen;   // Don't do a TreePush() when open (e.g. for CollapsingHeader) = no extra indent nor pushing on ID stack
			public static int ImGuiTreeNodeFlags_NoAutoOpenOnLog;   // Don't automatically and temporarily open node when Logging is active (by default logging will automatically ...
			public static int ImGuiTreeNodeFlags_DefaultOpen;   // Default node to be open
			public static int ImGuiTreeNodeFlags_OpenOnDoubleClick;   // Open on double-click instead of simple click (default for multi-select unless any _OpenOnXXX behavior is set ...
			public static int ImGuiTreeNodeFlags_OpenOnArrow;   // Open when clicking on the arrow part (default for multi-select unless any _OpenOnXXX behavior is set ...
			public static int ImGuiTreeNodeFlags_Leaf;   // No collapsing, no arrow (use as a convenience for leaf nodes). Note: will always open a tree/id scope and ...
			public static int ImGuiTreeNodeFlags_Bullet;   // Display a bullet instead of arrow. IMPORTANT: node can still be marked open/close if you don't set the _Leaf ...
			public static int ImGuiTreeNodeFlags_FramePadding;   // Use FramePadding (even for an unframed text node) to vertically align text baseline to regular widget height. ...
			public static int ImGuiTreeNodeFlags_SpanAvailWidth;   // Extend hit box to the right-most edge, even if not framed. This is not the default in order to allow adding ...
			public static int ImGuiTreeNodeFlags_SpanFullWidth;   // Extend hit box to the left-most and right-most edges (cover the indent area).
			public static int ImGuiTreeNodeFlags_SpanLabelWidth;   // Narrow hit box + narrow hovering highlight, will only cover the label text.
			public static int ImGuiTreeNodeFlags_SpanAllColumns;   // Frame will span all columns of its container table (label will still fit in current column)
			public static int ImGuiTreeNodeFlags_LabelSpanAllColumns;   // Label will span all columns of its container table
			public static int ImGuiTreeNodeFlags_NavLeftJumpsToParent;   // Nav: left arrow moves back to parent. This is processed in TreePop() when there's an unfulfilled Left nav ...
			public static int ImGuiTreeNodeFlags_CollapsingHeader;
			public static int ImGuiTreeNodeFlags_DrawLinesNone;   // No lines drawn
			public static int ImGuiTreeNodeFlags_DrawLinesFull;   // Horizontal lines to child nodes. Vertical line drawn down to TreePop() position: cover full contents. Faster ...
			public static int ImGuiTreeNodeFlags_DrawLinesToNodes;   // Horizontal lines to child nodes. Vertical line drawn down to bottom-most child node. Slower (for large trees).
			public static int ImGuiTreeNodeFlags_NavLeftJumpsBackHere;   // Renamed in 1.92.0

			static ImGuiTreeNodeFlags() { Init(ImGuiVersion.Version, ImGuiVersion.Docking); }

			// 1.93 and later get 1.93's numbers; anything older gets 1.92.5's.
			public static void Init(int version, bool docking)
			{
				if (version >= 19300) Init193(docking);
				else Init1925(docking);
			}

			// Dear ImGui 1.93 (ImGuiTreeNodeFlags_ in imgui.h)
			static void Init193(bool docking)
			{
				ImGuiTreeNodeFlags_None = 0;
				ImGuiTreeNodeFlags_Selected = 1 << 0;
				ImGuiTreeNodeFlags_Framed = 1 << 1;
				ImGuiTreeNodeFlags_AllowOverlap = 1 << 2;
				ImGuiTreeNodeFlags_NoTreePushOnOpen = 1 << 3;
				ImGuiTreeNodeFlags_NoAutoOpenOnLog = 1 << 4;
				ImGuiTreeNodeFlags_DefaultOpen = 1 << 5;
				ImGuiTreeNodeFlags_OpenOnDoubleClick = 1 << 6;
				ImGuiTreeNodeFlags_OpenOnArrow = 1 << 7;
				ImGuiTreeNodeFlags_Leaf = 1 << 8;
				ImGuiTreeNodeFlags_Bullet = 1 << 9;
				ImGuiTreeNodeFlags_FramePadding = 1 << 10;
				ImGuiTreeNodeFlags_SpanAvailWidth = 1 << 11;
				ImGuiTreeNodeFlags_SpanFullWidth = 1 << 12;
				ImGuiTreeNodeFlags_SpanLabelWidth = 1 << 13;
				ImGuiTreeNodeFlags_SpanAllColumns = 1 << 14;
				ImGuiTreeNodeFlags_LabelSpanAllColumns = 1 << 15;
				ImGuiTreeNodeFlags_NavLeftJumpsToParent = 1 << 17;
				ImGuiTreeNodeFlags_CollapsingHeader = ImGuiTreeNodeFlags_Framed | ImGuiTreeNodeFlags_NoTreePushOnOpen | ImGuiTreeNodeFlags_NoAutoOpenOnLog;
				ImGuiTreeNodeFlags_DrawLinesNone = 1 << 18;
				ImGuiTreeNodeFlags_DrawLinesFull = 1 << 19;
				ImGuiTreeNodeFlags_DrawLinesToNodes = 1 << 20;
				ImGuiTreeNodeFlags_NavLeftJumpsBackHere = ImGuiTreeNodeFlags_NavLeftJumpsToParent;
			}

			// Dear ImGui 1.92.5 (ImGuiTreeNodeFlags_ in imgui.h)
			static void Init1925(bool docking)
			{
				ImGuiTreeNodeFlags_None = 0;
				ImGuiTreeNodeFlags_Selected = 1 << 0;
				ImGuiTreeNodeFlags_Framed = 1 << 1;
				ImGuiTreeNodeFlags_AllowOverlap = 1 << 2;
				ImGuiTreeNodeFlags_NoTreePushOnOpen = 1 << 3;
				ImGuiTreeNodeFlags_NoAutoOpenOnLog = 1 << 4;
				ImGuiTreeNodeFlags_DefaultOpen = 1 << 5;
				ImGuiTreeNodeFlags_OpenOnDoubleClick = 1 << 6;
				ImGuiTreeNodeFlags_OpenOnArrow = 1 << 7;
				ImGuiTreeNodeFlags_Leaf = 1 << 8;
				ImGuiTreeNodeFlags_Bullet = 1 << 9;
				ImGuiTreeNodeFlags_FramePadding = 1 << 10;
				ImGuiTreeNodeFlags_SpanAvailWidth = 1 << 11;
				ImGuiTreeNodeFlags_SpanFullWidth = 1 << 12;
				ImGuiTreeNodeFlags_SpanLabelWidth = 1 << 13;
				ImGuiTreeNodeFlags_SpanAllColumns = 1 << 14;
				ImGuiTreeNodeFlags_LabelSpanAllColumns = 1 << 15;
				ImGuiTreeNodeFlags_NavLeftJumpsToParent = 1 << 17;
				ImGuiTreeNodeFlags_CollapsingHeader = ImGuiTreeNodeFlags_Framed | ImGuiTreeNodeFlags_NoTreePushOnOpen | ImGuiTreeNodeFlags_NoAutoOpenOnLog;
				ImGuiTreeNodeFlags_DrawLinesNone = 1 << 18;
				ImGuiTreeNodeFlags_DrawLinesFull = 1 << 19;
				ImGuiTreeNodeFlags_DrawLinesToNodes = 1 << 20;
				ImGuiTreeNodeFlags_NavLeftJumpsBackHere = ImGuiTreeNodeFlags_NavLeftJumpsToParent;
			}
		}

		public static class ImGuiTableFlags
		{
			// Features
			public static int ImGuiTableFlags_None;
			public static int ImGuiTableFlags_Resizable;   // Enable resizing columns.
			public static int ImGuiTableFlags_Reorderable;   // Enable reordering columns in header row. (Need calling TableSetupColumn() + TableHeadersRow() to display ...
			public static int ImGuiTableFlags_Hideable;   // Enable hiding/disabling columns in context menu.
			public static int ImGuiTableFlags_Sortable;   // Enable sorting. Call TableGetSortSpecs() to obtain sort specs. Also see ImGuiTableFlags_SortMulti and ...
			public static int ImGuiTableFlags_NoSavedSettings;   // Disable persisting columns order, width, visibility and sort settings in the .ini file.
			public static int ImGuiTableFlags_ContextMenuInBody;   // Right-click on columns body/contents will also display table context menu. By default it is available in ...
			// Decorations
			public static int ImGuiTableFlags_RowBg;   // Set each RowBg color with ImGuiCol_TableRowBg or ImGuiCol_TableRowBgAlt (equivalent of calling ...
			public static int ImGuiTableFlags_BordersInnerH;   // Draw horizontal borders between rows.
			public static int ImGuiTableFlags_BordersOuterH;   // Draw horizontal borders at the top and bottom.
			public static int ImGuiTableFlags_BordersInnerV;   // Draw vertical borders between columns.
			public static int ImGuiTableFlags_BordersOuterV;   // Draw vertical borders on the left and right sides.
			public static int ImGuiTableFlags_BordersH;   // Draw horizontal borders.
			public static int ImGuiTableFlags_BordersV;   // Draw vertical borders.
			public static int ImGuiTableFlags_BordersInner;   // Draw inner borders.
			public static int ImGuiTableFlags_BordersOuter;   // Draw outer borders.
			public static int ImGuiTableFlags_Borders;   // Draw all borders.
			public static int ImGuiTableFlags_NoBordersInBody;   // [ALPHA] Disable vertical borders in columns Body (borders will always appear in Headers). -> May move to style
			public static int ImGuiTableFlags_NoBordersInBodyUntilResize;   // [ALPHA] Disable vertical borders in columns Body until hovered for resize (borders will always appear in ...
			// Sizing Policy (read above for defaults)
			public static int ImGuiTableFlags_SizingFixedFit;   // Columns default to _WidthFixed or _WidthAuto (if resizable or not resizable), matching contents width.
			public static int ImGuiTableFlags_SizingFixedSame;   // Columns default to _WidthFixed or _WidthAuto (if resizable or not resizable), matching the maximum contents ...
			public static int ImGuiTableFlags_SizingStretchProp;   // Columns default to _WidthStretch with default weights proportional to each columns contents widths.
			public static int ImGuiTableFlags_SizingStretchSame;   // Columns default to _WidthStretch with default weights all equal, unless overridden by TableSetupColumn().
			// Sizing Extra Options
			public static int ImGuiTableFlags_NoHostExtendX;   // Make outer width auto-fit to columns, overriding outer_size.x value. Only available when ScrollX/ScrollY are ...
			public static int ImGuiTableFlags_NoHostExtendY;   // Make outer height stop exactly at outer_size.y (prevent auto-extending table past the limit). Only available ...
			public static int ImGuiTableFlags_NoKeepColumnsVisible;   // Disable keeping column always minimally visible when ScrollX is off and table gets too small. Not recommended ...
			public static int ImGuiTableFlags_PreciseWidths;   // Disable distributing remainder width to stretched columns (width allocation on a 100-wide table with 3 ...
			// Clipping
			public static int ImGuiTableFlags_NoClip;   // Disable clipping rectangle for every individual columns (reduce draw command count, items will be able to ...
			// Padding
			public static int ImGuiTableFlags_PadOuterX;   // Default if BordersOuterV is on. Enable outermost padding. Generally desirable if you have headers.
			public static int ImGuiTableFlags_NoPadOuterX;   // Default if BordersOuterV is off. Disable outermost padding.
			public static int ImGuiTableFlags_NoPadInnerX;   // Disable inner padding between columns (double inner padding if BordersOuterV is on, single inner padding if ...
			// Scrolling
			public static int ImGuiTableFlags_ScrollX;   // Enable horizontal scrolling. Require 'outer_size' parameter of BeginTable() to specify the container size. ...
			public static int ImGuiTableFlags_ScrollY;   // Enable vertical scrolling. Require 'outer_size' parameter of BeginTable() to specify the container size.
			// Sorting
			public static int ImGuiTableFlags_SortMulti;   // Hold shift when clicking headers to sort on multiple column. TableGetSortSpecs() may return specs where ...
			public static int ImGuiTableFlags_SortTristate;   // Allow no sorting, disable default sorting. TableGetSortSpecs() may return specs where (SpecsCount == 0).
			// Miscellaneous
			public static int ImGuiTableFlags_HighlightHoveredColumn;   // Highlight column headers when hovered (may evolve into a fuller highlight)
			// [Internal] Combinations and masks
			public static int ImGuiTableFlags_SizingMask_;

			static ImGuiTableFlags() { Init(ImGuiVersion.Version, ImGuiVersion.Docking); }

			// 1.93 and later get 1.93's numbers; anything older gets 1.92.5's.
			public static void Init(int version, bool docking)
			{
				if (version >= 19300) Init193(docking);
				else Init1925(docking);
			}

			// Dear ImGui 1.93 (ImGuiTableFlags_ in imgui.h)
			static void Init193(bool docking)
			{
				ImGuiTableFlags_None = 0;
				ImGuiTableFlags_Resizable = 1 << 0;
				ImGuiTableFlags_Reorderable = 1 << 1;
				ImGuiTableFlags_Hideable = 1 << 2;
				ImGuiTableFlags_Sortable = 1 << 3;
				ImGuiTableFlags_NoSavedSettings = 1 << 4;
				ImGuiTableFlags_ContextMenuInBody = 1 << 5;
				ImGuiTableFlags_RowBg = 1 << 6;
				ImGuiTableFlags_BordersInnerH = 1 << 7;
				ImGuiTableFlags_BordersOuterH = 1 << 8;
				ImGuiTableFlags_BordersInnerV = 1 << 9;
				ImGuiTableFlags_BordersOuterV = 1 << 10;
				ImGuiTableFlags_BordersH = ImGuiTableFlags_BordersInnerH | ImGuiTableFlags_BordersOuterH;
				ImGuiTableFlags_BordersV = ImGuiTableFlags_BordersInnerV | ImGuiTableFlags_BordersOuterV;
				ImGuiTableFlags_BordersInner = ImGuiTableFlags_BordersInnerV | ImGuiTableFlags_BordersInnerH;
				ImGuiTableFlags_BordersOuter = ImGuiTableFlags_BordersOuterV | ImGuiTableFlags_BordersOuterH;
				ImGuiTableFlags_Borders = ImGuiTableFlags_BordersInner | ImGuiTableFlags_BordersOuter;
				ImGuiTableFlags_NoBordersInBody = 1 << 11;
				ImGuiTableFlags_NoBordersInBodyUntilResize = 1 << 12;
				ImGuiTableFlags_SizingFixedFit = 1 << 13;
				ImGuiTableFlags_SizingFixedSame = 2 << 13;
				ImGuiTableFlags_SizingStretchProp = 3 << 13;
				ImGuiTableFlags_SizingStretchSame = 4 << 13;
				ImGuiTableFlags_NoHostExtendX = 1 << 16;
				ImGuiTableFlags_NoHostExtendY = 1 << 17;
				ImGuiTableFlags_NoKeepColumnsVisible = 1 << 18;
				ImGuiTableFlags_PreciseWidths = 1 << 19;
				ImGuiTableFlags_NoClip = 1 << 20;
				ImGuiTableFlags_PadOuterX = 1 << 21;
				ImGuiTableFlags_NoPadOuterX = 1 << 22;
				ImGuiTableFlags_NoPadInnerX = 1 << 23;
				ImGuiTableFlags_ScrollX = 1 << 24;
				ImGuiTableFlags_ScrollY = 1 << 25;
				ImGuiTableFlags_SortMulti = 1 << 26;
				ImGuiTableFlags_SortTristate = 1 << 27;
				ImGuiTableFlags_HighlightHoveredColumn = 1 << 28;
				ImGuiTableFlags_SizingMask_ = ImGuiTableFlags_SizingFixedFit | ImGuiTableFlags_SizingFixedSame | ImGuiTableFlags_SizingStretchProp | ImGuiTableFlags_SizingStretchSame;
			}

			// Dear ImGui 1.92.5 (ImGuiTableFlags_ in imgui.h)
			static void Init1925(bool docking)
			{
				ImGuiTableFlags_None = 0;
				ImGuiTableFlags_Resizable = 1 << 0;
				ImGuiTableFlags_Reorderable = 1 << 1;
				ImGuiTableFlags_Hideable = 1 << 2;
				ImGuiTableFlags_Sortable = 1 << 3;
				ImGuiTableFlags_NoSavedSettings = 1 << 4;
				ImGuiTableFlags_ContextMenuInBody = 1 << 5;
				ImGuiTableFlags_RowBg = 1 << 6;
				ImGuiTableFlags_BordersInnerH = 1 << 7;
				ImGuiTableFlags_BordersOuterH = 1 << 8;
				ImGuiTableFlags_BordersInnerV = 1 << 9;
				ImGuiTableFlags_BordersOuterV = 1 << 10;
				ImGuiTableFlags_BordersH = ImGuiTableFlags_BordersInnerH | ImGuiTableFlags_BordersOuterH;
				ImGuiTableFlags_BordersV = ImGuiTableFlags_BordersInnerV | ImGuiTableFlags_BordersOuterV;
				ImGuiTableFlags_BordersInner = ImGuiTableFlags_BordersInnerV | ImGuiTableFlags_BordersInnerH;
				ImGuiTableFlags_BordersOuter = ImGuiTableFlags_BordersOuterV | ImGuiTableFlags_BordersOuterH;
				ImGuiTableFlags_Borders = ImGuiTableFlags_BordersInner | ImGuiTableFlags_BordersOuter;
				ImGuiTableFlags_NoBordersInBody = 1 << 11;
				ImGuiTableFlags_NoBordersInBodyUntilResize = 1 << 12;
				ImGuiTableFlags_SizingFixedFit = 1 << 13;
				ImGuiTableFlags_SizingFixedSame = 2 << 13;
				ImGuiTableFlags_SizingStretchProp = 3 << 13;
				ImGuiTableFlags_SizingStretchSame = 4 << 13;
				ImGuiTableFlags_NoHostExtendX = 1 << 16;
				ImGuiTableFlags_NoHostExtendY = 1 << 17;
				ImGuiTableFlags_NoKeepColumnsVisible = 1 << 18;
				ImGuiTableFlags_PreciseWidths = 1 << 19;
				ImGuiTableFlags_NoClip = 1 << 20;
				ImGuiTableFlags_PadOuterX = 1 << 21;
				ImGuiTableFlags_NoPadOuterX = 1 << 22;
				ImGuiTableFlags_NoPadInnerX = 1 << 23;
				ImGuiTableFlags_ScrollX = 1 << 24;
				ImGuiTableFlags_ScrollY = 1 << 25;
				ImGuiTableFlags_SortMulti = 1 << 26;
				ImGuiTableFlags_SortTristate = 1 << 27;
				ImGuiTableFlags_HighlightHoveredColumn = 1 << 28;
				ImGuiTableFlags_SizingMask_ = ImGuiTableFlags_SizingFixedFit | ImGuiTableFlags_SizingFixedSame | ImGuiTableFlags_SizingStretchProp | ImGuiTableFlags_SizingStretchSame;
			}
		}

		public static class ImGuiTableColumnFlags
		{
			// Input configuration flags
			public static int ImGuiTableColumnFlags_None;
			public static int ImGuiTableColumnFlags_Disabled;   // Overriding/master disable flag: hide column, won't show in context menu (unlike calling ...
			public static int ImGuiTableColumnFlags_DefaultHide;   // Default as a hidden/disabled column.
			public static int ImGuiTableColumnFlags_DefaultSort;   // Default as a sorting column.
			public static int ImGuiTableColumnFlags_WidthStretch;   // Column will stretch. Preferable with horizontal scrolling disabled (default if table sizing policy is ...
			public static int ImGuiTableColumnFlags_WidthFixed;   // Column will not stretch. Preferable with horizontal scrolling enabled (default if table sizing policy is ...
			public static int ImGuiTableColumnFlags_NoResize;   // Disable manual resizing.
			public static int ImGuiTableColumnFlags_NoReorder;   // Disable manual reordering this column, this will also prevent other columns from crossing over this column.
			public static int ImGuiTableColumnFlags_NoHide;   // Disable ability to hide/disable this column.
			public static int ImGuiTableColumnFlags_NoClip;   // Disable clipping for this column (all NoClip columns will render in a same draw command).
			public static int ImGuiTableColumnFlags_NoSort;   // Disable ability to sort on this field (even if ImGuiTableFlags_Sortable is set on the table).
			public static int ImGuiTableColumnFlags_NoSortAscending;   // Disable ability to sort in the ascending direction.
			public static int ImGuiTableColumnFlags_NoSortDescending;   // Disable ability to sort in the descending direction.
			public static int ImGuiTableColumnFlags_NoHeaderLabel;   // TableHeadersRow() will submit an empty label for this column. Convenient for some small columns. Name will ...
			public static int ImGuiTableColumnFlags_NoHeaderWidth;   // Disable header text width contribution to automatic column width.
			public static int ImGuiTableColumnFlags_PreferSortAscending;   // Make the initial sort direction Ascending when first sorting on this column (default).
			public static int ImGuiTableColumnFlags_PreferSortDescending;   // Make the initial sort direction Descending when first sorting on this column.
			public static int ImGuiTableColumnFlags_IndentEnable;   // Use current Indent value when entering cell (default for column 0).
			public static int ImGuiTableColumnFlags_IndentDisable;   // Ignore current Indent value when entering cell (default for columns > 0). Indentation changes _within_ the ...
			public static int ImGuiTableColumnFlags_AngledHeader;   // TableHeadersRow() will submit an angled header row for this column. Note this will add an extra row.
			// Output status flags, read-only via TableGetColumnFlags()
			public static int ImGuiTableColumnFlags_IsEnabled;   // Status: is enabled == not hidden by user/api (referred to as "Hide" in _DefaultHide and _NoHide) flags.
			public static int ImGuiTableColumnFlags_IsVisible;   // Status: is visible == is enabled AND not clipped by scrolling.
			public static int ImGuiTableColumnFlags_IsSorted;   // Status: is currently part of the sort specs
			public static int ImGuiTableColumnFlags_IsHovered;   // Status: is hovered by mouse
			// [Internal] Combinations and masks
			public static int ImGuiTableColumnFlags_WidthMask_;
			public static int ImGuiTableColumnFlags_IndentMask_;
			public static int ImGuiTableColumnFlags_StatusMask_;
			public static int ImGuiTableColumnFlags_NoDirectResize_;   // [Internal] Disable user resizing this column directly (it may however we resized indirectly from its left ...
			public static int ImGuiTableColumnFlags_WidthAlwaysAutoResize;   // [1.92.5 only]

			static ImGuiTableColumnFlags() { Init(ImGuiVersion.Version, ImGuiVersion.Docking); }

			// 1.93 and later get 1.93's numbers; anything older gets 1.92.5's.
			public static void Init(int version, bool docking)
			{
				if (version >= 19300) Init193(docking);
				else Init1925(docking);
			}

			// Dear ImGui 1.93 (ImGuiTableColumnFlags_ in imgui.h)
			static void Init193(bool docking)
			{
				// not in 1.93
				ImGuiTableColumnFlags_WidthAlwaysAutoResize = 0;
				ImGuiTableColumnFlags_None = 0;
				ImGuiTableColumnFlags_Disabled = 1 << 0;
				ImGuiTableColumnFlags_DefaultHide = 1 << 1;
				ImGuiTableColumnFlags_DefaultSort = 1 << 2;
				ImGuiTableColumnFlags_WidthStretch = 1 << 3;
				ImGuiTableColumnFlags_WidthFixed = 1 << 4;
				ImGuiTableColumnFlags_NoResize = 1 << 5;
				ImGuiTableColumnFlags_NoReorder = 1 << 6;
				ImGuiTableColumnFlags_NoHide = 1 << 7;
				ImGuiTableColumnFlags_NoClip = 1 << 8;
				ImGuiTableColumnFlags_NoSort = 1 << 9;
				ImGuiTableColumnFlags_NoSortAscending = 1 << 10;
				ImGuiTableColumnFlags_NoSortDescending = 1 << 11;
				ImGuiTableColumnFlags_NoHeaderLabel = 1 << 12;
				ImGuiTableColumnFlags_NoHeaderWidth = 1 << 13;
				ImGuiTableColumnFlags_PreferSortAscending = 1 << 14;
				ImGuiTableColumnFlags_PreferSortDescending = 1 << 15;
				ImGuiTableColumnFlags_IndentEnable = 1 << 16;
				ImGuiTableColumnFlags_IndentDisable = 1 << 17;
				ImGuiTableColumnFlags_AngledHeader = 1 << 18;
				ImGuiTableColumnFlags_IsEnabled = 1 << 24;
				ImGuiTableColumnFlags_IsVisible = 1 << 25;
				ImGuiTableColumnFlags_IsSorted = 1 << 26;
				ImGuiTableColumnFlags_IsHovered = 1 << 27;
				ImGuiTableColumnFlags_WidthMask_ = ImGuiTableColumnFlags_WidthStretch | ImGuiTableColumnFlags_WidthFixed;
				ImGuiTableColumnFlags_IndentMask_ = ImGuiTableColumnFlags_IndentEnable | ImGuiTableColumnFlags_IndentDisable;
				ImGuiTableColumnFlags_StatusMask_ = ImGuiTableColumnFlags_IsEnabled | ImGuiTableColumnFlags_IsVisible | ImGuiTableColumnFlags_IsSorted | ImGuiTableColumnFlags_IsHovered;
				ImGuiTableColumnFlags_NoDirectResize_ = 1 << 30;
			}

			// Dear ImGui 1.92.5 (ImGuiTableColumnFlags_ in imgui.h)
			static void Init1925(bool docking)
			{
				ImGuiTableColumnFlags_None = 0;
				ImGuiTableColumnFlags_Disabled = 1 << 0;
				ImGuiTableColumnFlags_DefaultHide = 1 << 1;
				ImGuiTableColumnFlags_DefaultSort = 1 << 2;
				ImGuiTableColumnFlags_WidthStretch = 1 << 3;
				ImGuiTableColumnFlags_WidthFixed = 1 << 4;
				ImGuiTableColumnFlags_NoResize = 1 << 5;
				ImGuiTableColumnFlags_NoReorder = 1 << 6;
				ImGuiTableColumnFlags_NoHide = 1 << 7;
				ImGuiTableColumnFlags_NoClip = 1 << 8;
				ImGuiTableColumnFlags_NoSort = 1 << 9;
				ImGuiTableColumnFlags_NoSortAscending = 1 << 10;
				ImGuiTableColumnFlags_NoSortDescending = 1 << 11;
				ImGuiTableColumnFlags_NoHeaderLabel = 1 << 12;
				ImGuiTableColumnFlags_NoHeaderWidth = 1 << 13;
				ImGuiTableColumnFlags_PreferSortAscending = 1 << 14;
				ImGuiTableColumnFlags_PreferSortDescending = 1 << 15;
				ImGuiTableColumnFlags_IndentEnable = 1 << 16;
				ImGuiTableColumnFlags_IndentDisable = 1 << 17;
				ImGuiTableColumnFlags_AngledHeader = 1 << 18;
				ImGuiTableColumnFlags_IsEnabled = 1 << 24;
				ImGuiTableColumnFlags_IsVisible = 1 << 25;
				ImGuiTableColumnFlags_IsSorted = 1 << 26;
				ImGuiTableColumnFlags_IsHovered = 1 << 27;
				ImGuiTableColumnFlags_WidthMask_ = ImGuiTableColumnFlags_WidthStretch | ImGuiTableColumnFlags_WidthFixed;
				ImGuiTableColumnFlags_IndentMask_ = ImGuiTableColumnFlags_IndentEnable | ImGuiTableColumnFlags_IndentDisable;
				ImGuiTableColumnFlags_StatusMask_ = ImGuiTableColumnFlags_IsEnabled | ImGuiTableColumnFlags_IsVisible | ImGuiTableColumnFlags_IsSorted | ImGuiTableColumnFlags_IsHovered;
				ImGuiTableColumnFlags_NoDirectResize_ = 1 << 30;
				ImGuiTableColumnFlags_WidthAlwaysAutoResize = (ImGuiTableColumnFlags_WidthFixed | ImGuiTableColumnFlags_NoResize);
			}
		}

		public static uint GetColor(uint r, uint g, uint b, uint a)
		{
			return (a << 24) | (b << 16) | (g << 8) | r;
		}
		public static uint GetColor(float r, float g, float b, float a)
		{
			uint i_r = (uint)(r*255);
			uint i_g = (uint)(g*255);
			uint i_b = (uint)(b*255);
			uint i_a = (uint)(a*255);
			return (i_a << 24) | (i_b << 16) | (i_g << 8) | i_r;
		}
		public static float[] GetRGBAFloatsFromColor(UInt32 packedColor)
		{
			float[] r = new float[4];
			r[0] = ((float)((packedColor >> 0) & 0xFF))/ (float)255;
			r[1] = (float)((packedColor >> 8) & 0xFF) /  (float) 255;
			r[2] = (float)((packedColor >> 16) & 0xFF)/ (float)255;
			r[3] = (float)((packedColor >> 24) & 0xFF)/ (float)255;

			return r;
		}
		public static Int32[] GetRGBAIntsFromColor(UInt32 packedColor)
		{
			Int32[] r = new Int32[4];
			r[0] = (byte)((packedColor >> 0) & 0xFF);
			r[1] = (byte)((packedColor >> 8) & 0xFF);
			r[2] = (byte)((packedColor >> 16) & 0xFF);
			r[3] = (byte)((packedColor >> 24) & 0xFF);
			return r;
		}

		public static UITheme _currentTheme = UITheme.DarkTeal;

		private static readonly int _themePushCount = 27;
		// Rounding settings
		public static float GlobalRoundingValue = 6.0f;
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
		public extern static bool imgui_InvisibleButton(string name, float width, float height,int flags);
				
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
		// HSMono's own (not in MQ2Mono): the Dear ImGui the plugin draws with, for ImGuiVersion.Init.
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static int imgui_GetVersionNum();   // IMGUI_VERSION_NUM, e.g. 19300
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_HasDocking();
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
		public extern static void imgui_TableHeader(string label);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static string imgui_TableGetColumnName(int columnIndex);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_TableNextRow();

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_TableNextColumn();

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_TableSetColumnIndex(int index);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_PushID(int id);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_PopID();

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_PushItemWidth(float width);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_PopItemWidth();


		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_PushFont(string name, float font_size);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_PushEQFont(int fontid,float size);


		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_PopFont();


	

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_TextColored(float r, float g, float b, float a, string text);
		// Colors / styled text
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_TextUnformatted(string text);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_CalcTextSizeX(string text);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float[] imgui_CalcTextSize(string text);

		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern static void imgui_PushStyleColor(int which, float r, float g, float b, float a);
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern static void imgui_PopStyleColor(int count);
		// Style vars (rounding, padding, etc.)
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern static void imgui_PushStyleVarFloat(int which, float value);
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern static void imgui_PushStyleVarVec2(int which, float x, float y);
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern static void imgui_PopStyleVar(int count);
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
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_IsMouseDoubleClicked(int button);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_IsItemClicked(int button);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_SetClipboardText(string text);

		//
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
		public extern static float imgui_GetTextLineHeight();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetFrameHeight();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_GetWindowDrawList_AddRectFilled(float x1, float y1, float x2, float y2, uint color, float rounding = 0.0f, int rounding_corners_flags = 0);   // 0: every corner, when rounding > 0
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_GetWindowDrawList_AddRect(float p_min_x, float p_min_y, float p_max_x, float p_max_y, uint color, float rounding = 0.0f, int rounding_corners_flags = 0, float thickness=1.0f);   // 0: every corner, when rounding > 0
	
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_GetWindowDrawList_AddText(float x, float y, uint color, string text);
		// Item rect + color helpers
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float[] imgui_GetItemRectMin();
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

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_ColorPicker4(string label, int r, int g, int b, int a, int flags);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static int[] imgui_ColorPicker_GetRGBA(string label);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_ColorPicker4_Float(string label, float r, float g, float b, float a, int flags);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float[] imgui_ColorPicker_GetRGBA_Float(string label);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_ColorPicker_Clear(string label);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_ProgressBar(float fraction, float height, float width, string overlay);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_ProgressBarGradient(float fraction, float height, float width, uint color_start,uint color_end);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_GetWindowDrawList_AddRectFilledMultiColor(float p_min_x, float p_min_y, float p_max_x, float p_max_y, uint col_upr_left, uint col_upr_right,uint col_bot_right, uint col_bot_left);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_GetWindowDrawList_AddQuad(float p1_x, float p1_y, float p2_x, float p2_y, float p3_x, float p3_y, float p4_x, float p4_y, uint col, float thickness);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_GetWindowDrawList_AddQuadFilled(float p1_x, float p1_y, float p2_x, float p2_y, float p3_x, float p3_y, float p4_x, float p4_y, uint col);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_GetWindowDrawList_AddTriangle(float p1_x, float p1_y, float p2_x, float p2_y, float p3_x, float p3_y, uint col, float thickness);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_GetWindowDrawList_AddTriangleFilled(float p1_x, float p1_y, float p2_x, float p2_y, float p3_x, float p3_y, uint col);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_GetWindowDrawList_AddCircle(float center_x, float center_y, float radius, uint col, int num_segments, float thickness);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_GetWindowDrawList_AddCircleFilled(float center_x, float center_y, float radius, uint col, int num_segments);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_GetWindowDrawList_AddNgon(float center_x, float center_y, float radius, uint col, int num_segments, float thickness);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_GetWindowDrawList_AddNgonFilled(float center_x, float center_y, float radius, uint col, int num_segments);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_GetWindowDrawList_AddEllipse(float center_x, float center_y, float radius_x, float radius_y, uint col, float rot, int num_segments, float thickness);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_GetWindowDrawList_AddEllipseFilled(float center_x, float center_y, float radius_x, float radius_y, uint col, float rot, int num_segments);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_GetWindowDrawList_AddBezierCubic(float p1_x, float p1_y, float p2_x, float p2_y, float p3_x, float p3_y, float p4_x, float p4_y, uint col, float thickness, int num_segments);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_GetWindowDrawList_AddBezierQuadratic(float p1_x, float p1_y, float p2_x, float p2_y, float p3_x, float p3_y, uint col, float thickness, int num_segments);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_GetStyleVarVec2(int which, out int x, out int y);


		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float[] imgui_GetItemRectSize();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float[] imgui_GetStyleColorVec4(int flag);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float[] imgui_GetWindowDrawList_AddLine(float x, float y, float x_end, float y_end, uint color, float thickness = 1.0f);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_Internal_ItemSize(float x, float y, float text_baseline_y=1f);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_Internal_ItemAdd(float x, float y, float x2, float y2);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_Internal_CalcItemSize(float x, float y, float default_w, out float r_x, out float r_y);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetStyleVarFloat(int which);

		#endregion

		private static void PushCommonRounding()
		{
			// Apply consistent rounding across key style vars
			imgui_PushStyleVarFloat((int)ImGuiStyleVar.WindowRounding, GlobalRoundingValue);
			imgui_PushStyleVarFloat((int)ImGuiStyleVar.ChildRounding, GlobalRoundingValue);
			imgui_PushStyleVarFloat((int)ImGuiStyleVar.PopupRounding, GlobalRoundingValue);
			imgui_PushStyleVarFloat((int)ImGuiStyleVar.FrameRounding, GlobalRoundingValue);
			imgui_PushStyleVarFloat((int)ImGuiStyleVar.GrabRounding, Math.Max(3.0f, GlobalRoundingValue - 2.0f));
			imgui_PushStyleVarFloat((int)ImGuiStyleVar.TabRounding, GlobalRoundingValue);
			imgui_PushStyleVarFloat((int)ImGuiStyleVar.ScrollbarRounding, GlobalRoundingValue);
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
			public bool BeginPopupContextItemPerf(string type,string idString,Int32 id, int flags)
			{
				string rowID = String.Empty;
				ValueStringBuilder sb = new ValueStringBuilder();
				try
				{
					sb.Append(idString);
					sb.Append(e3util.GetIntStr(id));
					rowID = StringPool.Shared.GetOrAdd(sb.AsSpan());
				}
				finally
				{
					sb.Dispose();
				}
				
				IsOpen = imgui_BeginPopupContextItem(rowID, flags);
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
			private ImGUITable() { } //private constructor, needs to be created so that you are forced to use the pool.

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
			~ImGUITable() { }
		}
		public class ImGUIChild : IDisposable
		{
			public bool BeginChild(string id, float width, float height, int child_flags, int window_flags)
			{
				return imgui_BeginChild(id, width, height, child_flags, window_flags);
			}
			#region objectPoolingStuff
			//private constructor, needs to be created so that you are forced to use the pool.
			private ImGUIChild() { }
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
			~ImGUIChild(){}
			#endregion
		}
		public class PushStyle : IDisposable
		{
			Int32 styleColorsPushed = 0;
			Int32 styleVarPushed = 0;
			public void PushStyleColor(int type, float r, float g, float b, float a)
			{
				imgui_PushStyleColor(type, r, g, b, a);
				styleColorsPushed++;
			}
			public void PushStyleVarVec2(int flags, float x, float y)
			{
				imgui_PushStyleVarVec2(flags, 0, 0);
				styleVarPushed++;
			}
			public void PushStyleVarFloat(int flags, float value)
			{
				imgui_PushStyleVarFloat(flags, value);
				styleVarPushed++;
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
				if(styleColorsPushed>0)
				{
					imgui_PopStyleColor(styleColorsPushed);
				}
				styleColorsPushed = 0;

				if(styleVarPushed>0)
				{
					imgui_PopStyleVar(styleVarPushed);
				}
				styleVarPushed = 0;
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
		public class Push : IDisposable
		{
			
			Int32 ItemWidthCount = 0;
			public void PushItemWidth(float width)
			{
				imgui_PushItemWidth(width);
				ItemWidthCount++;
			}
		
			#region objectPoolingStuff
			//private constructor, needs to be created so that you are forced to use the pool.
			private Push()
			{

			}
			public static Push Aquire()
			{
				Push obj;
				if (!StaticObjectPool.TryPop<Push>(out obj))
				{
					obj = new Push();
				}

				return obj;
			}
			public void Dispose()
			{
				for(Int32 i = 0; i < ItemWidthCount; i++)
				{
					imgui_PopItemWidth();
				}
				ItemWidthCount = 0;
				StaticObjectPool.Push(this);
			}
			~Push()
			{
				//DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
				//if this is called, it will cause the domain to hang in the GC when shuttind down
				//This is only here to warn you

			}

			#endregion
		}
		static Dictionary<string, Int32> eqFontStringToInt = new Dictionary<string, Int32>() { { "0", 0 }, { "1", 1 }, { "2", 2 }, { "3", 3 }, { "4", 4 }, { "5", 5 }, { "6",6 }, { "7", 7 }, { "8", 8 }, { "9", 9 }, { "10", 10 } };
		public class IMGUI_Fonts : IDisposable
		{
			Int32 fontsApplied = 0;
			public void PushFont(string font)
			{
				if(font.StartsWith("EQ-"))
				{
					if (FontList.TryGetValue(font, out var fontIDAsStrings))
					{
						if (eqFontStringToInt.TryGetValue(fontIDAsStrings, out var fontid))
						{
							PushEQFont(fontid);
						}
					}
				}
				else
				{
					string fontToUse = font;
					if (FontList.ContainsKey(font))
					{
						fontToUse = FontList[font];
					}
					if (imgui_PushFont(fontToUse, 0))
					{
						fontsApplied++;
					}
				}
			}
			public void PushEQFont(Int32 fontid)
			{
				if(imgui_PushEQFont(fontid,0))
				{
					fontsApplied++;
				}
			}

			public void PushFontSize(float fontsize)
			{
				
				if (imgui_PushFont(String.Empty, fontsize))
				{
					fontsApplied++;
				}
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
				for(int i = 0;i<fontsApplied;i++)
				{
					imgui_PopFont();
				}
				fontsApplied = 0;
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
		private static Dictionary<Int32, String> _percentToString = new Dictionary<int, string>();

		public static string PercentString(Int32 value)
		{
			if (_percentToString.TryGetValue(value, out var returnValue))
			{
				return returnValue;
			}
			else
			{
				string percentString = value + "%";
				_percentToString.Add(value, percentString);
				return percentString;
			}
		}

		public static void ProgressBarGradient(float progress, float width, float height, uint color_start, uint color_end, bool showpercent = true)
		{


			float x = imgui_GetCursorScreenPosX();
			float y = imgui_GetCursorScreenPosY();

			float availX = imgui_GetContentRegionAvailX();
			float size_x = 0;
			float size_y = 0;

			float styleRounding = imgui_GetStyleVarFloat((int)ImGuiStyleVar.FrameRounding);
			imgui_Internal_CalcItemSize(width,height, availX,out size_x,out size_y);
			imgui_Internal_ItemSize(size_x, size_y, 1f);
			imgui_Internal_ItemAdd(x, y, (x + size_x), (y + size_y));

			uint bgColor = imgui_GetColorU32((int)ImGuiCol.FrameBg, 1);
			float progress_x = x + size_x * progress;
			//imgui_Text($"x:{x},y:{y},size_x:{size_x},size_y:{size_y}, bgcolor:{bgColor} progress_x:{progress_x}");

			string pecentString = PercentString((int)(progress * 100));
			var sizeOfPercent= imgui_CalcTextSize(pecentString);
			imgui_GetWindowDrawList_AddRectFilled(x, y, (x + size_x), (y + size_y), bgColor);
			if (progress_x > 0)
			{

				imgui_GetWindowDrawList_AddRectFilledMultiColor(x, y, progress_x, (y + size_y), color_start, color_end, color_end, color_start);
			
				if (showpercent) imgui_GetWindowDrawList_AddText(x+(size_x/2 - (sizeOfPercent[0] / 2)), y, GetColor(255, 255, 255, 255), pecentString);
			}
			else
			{
				if (showpercent) imgui_GetWindowDrawList_AddText(x+(size_x/2 - (sizeOfPercent[0]/2)), y, GetColor(255, 255, 255, 255), "0%");
			}
		}
	}
}
