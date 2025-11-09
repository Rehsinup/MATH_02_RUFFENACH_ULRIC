using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Exercice : MonoBehaviour
{
    [SerializeField] private Transform _cube = null;
    [SerializeField][Range(0f, 1f)] private float _t = 0.0f;

    [SerializeField] private Vector3 _startPosition = Vector3.zero;
    [SerializeField] private Vector3 _endPosition = Vector3.zero;

    [SerializeField] private Vector3 _startRotation = Vector3.zero;
    [SerializeField] private Vector3 _endRotation = Vector3.zero;

    [SerializeField] private AnimationCurve _curve = null;

    private void Update()
    {
        //On passe T dans une courbe pour le rendre non linéaire
        float nonLinearT = _curve.Evaluate(_t);
        //On effectue une interpolation entre la position de départ et d'arrivée en fonction de T
        //Et on l'applique à la position du cube
        _cube.position = Vector3.LerpUnclamped(_startPosition, _endPosition, nonLinearT);
        //On effectue une interpolation entre la rotation de départ et d'arrivée en fonction de T
        //Et on l'applique à la rotation du cube
        _cube.rotation = Quaternion.Euler(Vector3.LerpUnclamped(_startRotation, _endRotation, nonLinearT));
    }

    /*void Start()
    {
        int result = Exercice01(new Vector3(0.0f, 1.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f));
        Debug.Log(result);

        Vector3 test1 = Vector3.zero;
        Vector3 test2 = Vector3.zero;

        float dot = Vector3.Dot(test1, test2);

        Vector3 cross = Vector3.Cross(test1, test2);

        test1.Normalize();

        Vector3 normalizedTest = test1.normalized;

        float len = test1.magnitude;
        float sqrLen = test1.sqrMagnitude;

        float sqrt = Mathf.Sqrt(sqrLen);
    }

    private int Exercice01(Vector3 vectorOne, Vector3 vectorTwo)
    {
        //On normalise les vecteurs pour obtenir des resultats de dot entre -1 et 1
        Vector3 normalizedVectorOne = vectorOne.normalized;
        Vector3 normalizedVectorTwo = vectorTwo.normalized;

        //Dot entre les deux vecteurs
        //Si dot = 1, ils sont alignés
        //Si dot = -1 ils sont opposés
        float dot = Vector3.Dot(normalizedVectorOne, normalizedVectorTwo);

        if (dot == 1.0f)
            return 1;

        if (dot == -1.0f)
            return -1;

        //On effectue un cross avec un vecteur sur la 3ème dimension pour obtenir les vecteurs de droite et de gauche par rapport au vectorOne
        Vector3 rightVector = Vector3.Cross(normalizedVectorOne, new Vector3(0.0f, 0.0f, 1.0f)); //Vector3.forward
        Vector3 leftVector = Vector3.Cross(normalizedVectorOne, new Vector3(0.0f, 0.0f, -1.0f)); // Vector3.back

        //Puis on effectue un dot entre ces deux vecteurs et vectorTwo pour savoir duquel il est le plus proche
        float rightDot = Vector3.Dot(normalizedVectorTwo, rightVector);
        float leftDot = Vector3.Dot(normalizedVectorTwo, leftVector);

        //Si le dot de droite est plus petit que celui de gauche, alors le vectorTwo se trouve à droite
        if (rightDot <= leftDot)
            return 1;

        //Sinon il se trouve à gauche
        return -1;
    }*/
}
