// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;
using UnityEngine.InputSystem.Interactions;

[RequireComponent( typeof(LineRenderer) )]
public class CurvedLineRenderer : MonoBehaviour 
{
	//PUBLIC
	public static CurvedLineRenderer Instance { get; private set; }
	public event EventHandler<OnMeasurementEventArgs> OnMeasurementEvent;
    public class OnMeasurementEventArgs : EventArgs
    {
        public float measurementValue;
    }


	public float lineSegmentSize = 0.15f;
	public float lineWidth = 0.1f;
	[Header("Gizmos")]
	public bool showGizmos = true;
	public float gizmoSize = 0.1f;
	public Color gizmoColor = new Color(1,0,0,0.5f);
	//PRIVATE
	private CurvedLinePoint[] linePoints = new CurvedLinePoint[0];
	private Vector3[] linePositions = new Vector3[0];
	private Vector3[] linePositionsOld = new Vector3[0];

	public Material material;

	private float lineLength = 0;

	private void Awake() {
		if (Instance == null) {
			Instance = this;	
		} else {
			Debug.Log("Duplicated Curved Line Renderer");
		}
	}

	private void Start() {
		InputActionsManager.Instance.InputActions.XRILeftHand.MenuButton.performed += MenuButton_Performed;
	}

    private void MenuButton_Performed(InputAction.CallbackContext context)
    {
        if (context.interaction is not HoldInteraction) {
			return;
		} 

		if (ToolsPanelUI.Instance.GetMode() != ToolsPanelUI.Modes.Measure || 
			(MeasurementToolsUI.Instance.GetMeasurementTypes() != MeasurementToolsUI.MeasurementTypes.Curved 
			&& MeasurementToolsUI.Instance.GetMeasurementTypes() != MeasurementToolsUI.MeasurementTypes.Linear
			&& MeasurementManager.Instance.GetCurrentLinearMeasurementMethod() != MeasurementManager.LinearMeasurementMethods.TwoHands)) 
		{
				return;
		}

		if (lineLength == 0) {
			return;
		}	

		OnMeasurementEvent?.Invoke(this, new OnMeasurementEventArgs {
			measurementValue = lineLength
		});
		
		ResetVector();
    }

    private void ResetVector()
    {
		foreach(CurvedLinePoint linePoint in linePoints) {
			Destroy(linePoint.gameObject);
		}

        linePoints = new CurvedLinePoint[0];
		linePositions = new Vector3[0];
		linePositionsOld = new Vector3[0];		
    }

    // Update is called once per frame

    public void Update () 
	{
		GetPoints();
		SetPointsToLine();
	}

	void GetPoints()
	{
		//find curved points in children
		linePoints = GetComponentsInChildren<CurvedLinePoint>();

		//add positions
		linePositions = new Vector3[linePoints.Length];
		for( int i = 0; i < linePoints.Length; i++ )
		{
			linePositions[i] = linePoints[i].transform.position;
		}
	}

	void SetPointsToLine()
	{
		//create old positions if they dont match
		if( linePositionsOld.Length != linePositions.Length )
		{
			linePositionsOld = new Vector3[linePositions.Length];
		}

		//check if line points have moved
		bool moved = false;
		for( int i = 0; i < linePositions.Length; i++ )
		{
			//compare
			if( linePositions[i] != linePositionsOld[i] )
			{
				moved = true;
			}
		}

		//update if moved
		if( moved == true )
		{
			LineRenderer line = this.GetComponent<LineRenderer>();

			//get smoothed values
			Vector3[] smoothedPoints = LineSmoother.SmoothLine( linePositions, lineSegmentSize );

			//set line settings
			line.positionCount = smoothedPoints.Length;
			line.SetPositions( smoothedPoints );
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
    		line.material = material;

			lineLength = CalculateLineLength(smoothedPoints);
            Debug.Log("Line Length: " + lineLength);
		}
	}

	float CalculateLineLength(Vector3[] points)
    {
        float length = 0f;
        for (int i = 1; i < points.Length; i++)
        {
            length += Vector3.Distance(points[i - 1], points[i]);
        }
        return length;
    }

	void OnDrawGizmosSelected()
	{
		Update();
	}

	void OnDrawGizmos()
	{
		if( linePoints.Length == 0 )
		{
			GetPoints();
		}

		//settings for gizmos
		foreach( CurvedLinePoint linePoint in linePoints )
		{
			linePoint.showGizmo = showGizmos;
			linePoint.gizmoSize = gizmoSize;
			linePoint.gizmoColor = gizmoColor;
		}
	}
}
