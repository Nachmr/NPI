   M S B A M L   `   `   `  ����  CoordinateMappingBasics-WPFA    ;Microsoft.Samples.Kinect.CoordinateMappingBasics.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   -   5        $��Window��5   	     � Title$  
Air Guitar��5   	   $	��770��6   $	��735��6(    � 	Resources 5         MediumGreyBrush �� Color  KinectPurpleBrush  KinectBlueBrush �� 
TargetType �� Property �� Value��%�   &	        &	 #     &	 F     '�� i     �� 5   
   $ 	#ff6e6e6e��62   �� 5   
   $ 	#ff52318f��64   �� 5	   
   $ 	#ff00BCF2��62   �� 5
   
   " ��6   .�5      ��  �@J�SnapsToDevicePixels . 64   5      5      .��5      � -   5        $��
layoutGrid��5      $��	10 0 10 0q�6    � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      �� 5      $
��Auto �6   $��26��6*   .+�5   
   � -   5        $
��Row1��5      $��1��6   $��3��6(   .+�5      �� -   5        $��Backdrop��5      $��Images\Background.png��6%   $��0,0,0,0q�6D   �� 5      -   5        $��MaskedColor��5      $��Stretch=�6(   $��Stretch=�6F   $��0,-10,-10,0q�6b   �� 5      -   5        $��
Seleccion1��5      $!��Images\Acusticselection.bmp��6'   $��643,140,27,506q�6L    J� Opacity$	 0.5Q�6d    J� RenderTransformOrigin$ -5.808,0.57&�6r   $y�Hidden=�6�   �� 5      -   5        $��
Seleccion2��5      $"��Images\Electricselection.png��6'   $��643,237,27,407q�6M   $ 1Q�6e   $ -5.808,0.57&�6q   $y�Hidden=�6�   �� 5      -   5        $��Midiendo��5      $��Images\posmedir.png��6%   $��10,123,673,453q�6B   $ 1Q�6Z   $ -5.808,0.57&�6f   $y�Visible=�6�   5      �� 5   
   -	   5        $��	statusBar��5   ?   $��4��6   $��Stretch=�6!   $��Bottom=�6P   
�������#��[  6~   $��26��6�   .@�5      �� $��0 0 0 10q�6   .��5!      �� -
   5        $��statusBarText��5!      $��-1 0 0 0q�61   Kinect Status6R   5"      5$      5%      5&      