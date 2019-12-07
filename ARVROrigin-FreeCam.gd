extends ARVROrigin

func _process(dt):
    var dir = Vector3(0,0,0);
    if (Input.is_key_pressed(KEY_W)):
        dir += Vector3(0,0,-1);
    if (Input.is_key_pressed(KEY_S)):
        dir += Vector3(0,0,1);
    if (Input.is_key_pressed(KEY_A)):
        dir += Vector3(-1,0,0);
    if (Input.is_key_pressed(KEY_D)):
        dir += Vector3(1,0,0);

    dir = $ARVRCamera.transform.basis.xform((dir));
    if (dir.length_squared() > 0.01):
        translation = translation + dir.normalized() * dt;
