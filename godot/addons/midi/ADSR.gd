extends AudioStreamPlayer

class_name AudioStreamPlayerADSR

const Bank = preload( "Bank.gd" )
const gap_second:float = 44100.0 / 1024.0 / 1000.0

"""
	AudioStreamPlayer with ADSR + Linked by Yui Kinomoto @arlez80
"""

# 発音チャンネル
var channel_number:int = -1
# 発音キーナンバー
var key_number:int = -1
# Hold 1
var hold:bool = false
# リリース中？
var releasing:bool = false
# リリース要求
var request_release:bool = false
# リリース開始ズラし
var request_release_second:float = 0.0
# 楽器情報
var instrument:Bank.Instrument = null
# 合成情報
var velocity:int = 0
var pitch_bend:float = 0.0
var pitch_bend_sensitivity:float = 12.0
var modulation:float = 0.0
var modulation_sensitivity:float = 0.5
var base_pitch:float = 0.0
# ADSRタイマー
var timer:float = 0.0
# 使用時間
var using_timer:float = 0.0
# リンク済の音色
onready var linked:AudioStreamPlayer = $Linked
var linked_base_pitch:float = 0.0
# 同時発音数
var polyphony_count:float = 1.0

# 現在のADSRボリューム
var current_volume_db:float = 0.0
# 自動リリースモード？
var auto_release_mode:bool = false

# 強制アップデート
var force_update:bool = false

# LinkedSampleを使用中
var is_check_using_linked:bool

# ADSステート
onready var ads_state:Array = [
	Bank.VolumeState.new( 0.0, 0.0 ),
	Bank.VolumeState.new( 0.2, -144.0 )
	# { "time": 0.2, "jump_to": 0.0 },	# not implemented
]
# Rステート
onready var release_state:Array = [
	Bank.VolumeState.new( 0.0, 0.0 ),
	Bank.VolumeState.new( 0.01, -144.0 )
	# { "time": 0.2, "jump_to": 0.0 },	# not implemented
]

func _ready( ):
	self.stop( )

func _check_using_linked( ) -> bool:
	return self.instrument != null and 2 <= len( self.instrument.array_stream )

func set_instrument( _instrument:Bank.Instrument ) -> void:
	if self.instrument == _instrument:
		return

	self.instrument = _instrument
	self.base_pitch = _instrument.array_base_pitch[0]
	self.stream = _instrument.array_stream[0]
	self.ads_state = _instrument.ads_state
	self.release_state = _instrument.release_state

	self.is_check_using_linked = self._check_using_linked( )
	if self.is_check_using_linked:
		self.linked_base_pitch = _instrument.array_base_pitch[1]
		self.linked.stream = _instrument.array_stream[1]

func play( from_position:float = 0.0 ):
	self.releasing = false
	self.request_release = false
	self.timer = 0.0
	self.using_timer = 0.0
	self.linked.bus = self.bus
	self.pitch_scale = 1.0
	self.linked.pitch_scale = 1.0

	self.current_volume_db = self.ads_state[0].volume_db
	self._update_volume( )

	var from_position_skip_silence:float = from_position + Bank.head_silent_second
	var mix_delay:float = clamp( self.gap_second - AudioServer.get_time_to_next_mix( ), 0.0, self.gap_second )
	var own_from_position:float = from_position_skip_silence - mix_delay * pow( 2.0, self.base_pitch )
	.play( max( 0.0, own_from_position ) )
	if self.is_check_using_linked:
		var linked_from_position:float = from_position_skip_silence - mix_delay * pow( 2.0, self.linked_base_pitch )
		self.linked.play( max( 0.0, linked_from_position ) )

	self.force_update = true
	self._update_adsr( 0.0 )
	self.force_update = false

func stop( ):
	.stop( )
	if self.linked != null:
		self.linked.stop( )
	self.hold = false

func start_release( ) -> void:
	self.request_release_second = self.gap_second - AudioServer.get_time_to_next_mix( )
	self.request_release = true

func _update_adsr( delta:float ) -> void:
	if ( not self.playing ) and ( not self.force_update ):
		return

	self.timer += delta
	self.using_timer += delta

	# ADSR選択
	var use_state = null
	if self.releasing:
		use_state = self.release_state
	else:
		use_state = self.ads_state

	var all_states:int = use_state.size( )
	var last_state:int = all_states - 1
	if use_state[last_state].time <= self.timer:
		self.current_volume_db = use_state[last_state].volume_db
		if self.releasing: self.stop( )
		if self.auto_release_mode: self.request_release = true
	else:
		for state_number in range( 1, all_states ):
			var state:Bank.VolumeState = use_state[state_number]
			if self.timer < state.time:
				var pre_state:Bank.VolumeState = use_state[state_number-1]
				var s:float = ( state.time - self.timer ) / ( state.time - pre_state.time )
				var t:float = 1.0 - s
				self.current_volume_db = pre_state.volume_db * s + state.volume_db * t
				break

	var synthed_pitch_bend:float = self.pitch_bend * self.pitch_bend_sensitivity / 12.0
	var synthed_modulation:float = sin( self.using_timer * 32.0 ) * ( self.modulation * self.modulation_sensitivity / 12.0 )
	self.pitch_scale = pow( 2.0, self.base_pitch + synthed_modulation + synthed_pitch_bend )
	if self.is_check_using_linked:
		self.linked.pitch_scale = pow( 2.0, self.linked_base_pitch + synthed_modulation + synthed_pitch_bend )

	self._update_volume( )

	if self.hold:
		pass
	else:
		if self.request_release and not self.releasing:
			self.request_release_second -= delta
			if self.request_release_second <= 0.0:
				self.releasing = true
				self.current_volume_db = self.release_state[0].volume_db
				self.timer = 0.0

func _update_volume( ) -> void:
	var v:float = self.current_volume_db + linear2db( float( self.velocity ) / 127.0 )# + self.instrument.volume_db

	if self.is_check_using_linked:
		v = max( -144.0, linear2db( db2linear( v ) / self.polyphony_count / 2.0 ) )
		self.volume_db = v
		self.linked.volume_db = v
	else:
		v = max( -144.0, linear2db( db2linear( v ) / self.polyphony_count ) )
		self.volume_db = v
