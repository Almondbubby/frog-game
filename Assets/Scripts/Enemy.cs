using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public Rigidbody2D rb;
    int dir = 1;
    public int speed = 60;
    void FixedUpdate()
    {
        Vector3 velo = new Vector3(1, 0, 0);
        velo = velo * speed * Time.deltaTime * dir;
        rb.velocity = velo;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        dir *= -1;
    }
}
