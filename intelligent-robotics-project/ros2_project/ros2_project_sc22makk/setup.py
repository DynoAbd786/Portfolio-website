from setuptools import find_packages, setup

package_name = 'ros2_project_sc22makk'

setup(
    name=package_name,
    version='0.0.0',
    packages=find_packages(exclude=['test']),
    data_files=[
        ('share/ament_index/resource_index/packages',
            ['resource/' + package_name]),
        ('share/' + package_name, ['package.xml']),
    ],
    install_requires=['setuptools'],
    zip_safe=True,
    maintainer='cscajb',
    maintainer_email='x.wang16@leeds.ac.uk',
    description='TODO: Package description',
    license='TODO: License declaration',
    tests_require=['pytest'],
    entry_points={
        'console_scripts': [
            'first_step = ros2_project_sc22makk.first_step:main',
            'second_step = ros2_project_sc22makk.second_step:main',
            'third_step = ros2_project_sc22makk.third_step:main',
            'fourth_step = ros2_project_sc22makk.fourth_step:main',
            # Add new entry points
            'color_detector = ros2_project_sc22makk.color_detector:main',
            'robot = ros2_project_sc22makk.robot:main',
            'motion_controller = ros2_project_sc22makk.motion_controller:main',
            'exploration_strategy = ros2_project_sc22makk.exploration_strategy:main',
        ],
    },
)
