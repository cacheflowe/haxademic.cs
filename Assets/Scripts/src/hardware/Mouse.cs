using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mouse : Object
{

  public static float xNorm() {
    return Input.mousePosition.x / Screen.width;
  }

  public static float yNorm() {
    return Input.mousePosition.y / Screen.height;
  }

  public static float xCentered() {
    return (xNorm() - 0.5f) * 2f; // -1 to 1
  }

  public static float yCentered() {
    return (yNorm() - 0.5f) * 2f; // -1 to 1
  }

  public static float xDelta() {
    return Input.GetAxis("Mouse X");
  }

  public static float yDelta() {
    return Input.GetAxis("Mouse Y");
  }

  static Vector3 mousePos;
  static Vector3 mouseCenterOffset = new Vector3(0.5f, 0.5f, 0.0f);
  public static Vector3 posCentered() {
    return Camera.main.ScreenToViewportPoint(Input.mousePosition) - mouseCenterOffset;
  }

}
