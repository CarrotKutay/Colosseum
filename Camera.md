# Camera
---
### Player Camera
The palyer camera is adjusted to the movement of the player itself it will always be on a fixed distance to the player and presents a 3rd person view of the character controlled. The distance is adjustable by an orbit multiplier and an offset. The orbit multiplier deals with the rotation of the camera around the player in yz-direction, while the offset mainly controls the y-height of the camera. This offset is not scaled by the player position as the camera should always be on a fixed upper (above the shoulder) perspective to follow the player behaviour.

### Making the camera work via an entity related system
The camera following the palyer will use an entity (cameraEntity) to copy all movements from as cameras as of yet are not integrated into the ECS workflow. Any movement and rotation to the camera will therefore be applied to  the cameraEntity first and then be copied by the actual camera. Movement and rotation can in this way be controlled by a separate camera entity system.
The values for the orbit multiplier and offset are adjusted via the camera tag component. The rotation however functions way to fast therefore the camera steering will have to be regulated and slowed down.
In the script [FollowEntity.cs](Assets\Scripts\PhysicsBasedMovement\FollowEntity.cs) we will perform the regulation by getting the current fowrad vector of the camera entity, regulating the distance between the current fowrad vector of the camera and the forward vector of the camera entity.

## Changelog
### Updates
#### Update 2.0: Rotation towards User-Input
Turn the camera entity towards player input directly. The player input will be be an x and y from 0 to 1/-1 respectively. With a rotation regulation threshold and regulation factor it will be possible to slow rotation according to need and possibly divide into steps, while only applying them when turn input excells the threshold given.
*Features*
- Uses non-linear transition between new camera positions to make transitions more smooth
#### Update 1.0: Rotation with quaternions
*Positive*
- easy rotation, not difficult to follow
- fast rotatíon
*Negative*
- Too fast -> instant rotation, when follwoing the entity camera with the real camera the visual is _stresssfull_
- Seperation of x and y rotation seems difficult (might be another issue alltogether, not quite visually debug-able)

*Conclusion*
Will put rotation with quaterninions on hold for now and go back to approach of working directly with the turnInput given by the player
